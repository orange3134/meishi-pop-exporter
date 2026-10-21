import hashlib
import json
from pathlib import Path
import tarfile
import tempfile
from unittest.mock import patch

import sync_from_app
import unittest
import zipfile

import build_release


class PackagingTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.temp = tempfile.TemporaryDirectory()
        cls.output = Path(cls.temp.name)
        cls.metadata = build_release.build(cls.output)
        cls.config = json.loads((build_release.ROOT / 'distribution.json').read_text())

    @classmethod
    def tearDownClass(cls):
        cls.temp.cleanup()

    def test_archives_preserve_guid_and_migration_roots(self):
        expected = {}
        for name, manifest in self.metadata['packages'].items():
            path = self.output / manifest['url'].rsplit('/', 1)[-1]
            self.assertEqual(hashlib.sha256(path.read_bytes()).hexdigest(), manifest['zipSHA256'])
            with zipfile.ZipFile(path) as archive:
                self.assertEqual(json.loads(archive.read('package.json'))['name'], name)
                self.assertNotIn('zipSHA256', json.loads(archive.read('package.json')))
                root = self.config['packages'][name]['legacyPath']
                for relative in archive.namelist():
                    if (relative.startswith(('Runtime/', 'Editor/')) and not relative.endswith('.meta')) or relative in ('LICENSE.txt', 'LICENSE_SCOPE.md'):
                        expected[root + '/' + relative] = (archive.read(relative), archive.read(relative + '.meta'))
        actual = {}
        roots = {}
        with tarfile.open(self.output / self.metadata['unitypackage']) as archive:
            for entry in archive.getmembers():
                self.assertTrue(entry.isfile())
                if not entry.name.endswith('/pathname'):
                    continue
                prefix = entry.name.split('/')[0]
                path = archive.extractfile(entry).read().decode()
                self.assertTrue(path.startswith('Assets/MEISHIPop/')) if path != 'Assets/MEISHIPop' else None
                meta = archive.extractfile(prefix + '/asset.meta').read()
                self.assertEqual(build_release.guid(meta), prefix)
                try:
                    actual[path] = (archive.extractfile(prefix + '/asset').read(), meta)
                except KeyError:
                    roots[path] = prefix
        self.assertEqual(actual, expected)
        for item in self.config['packages'].values():
            self.assertEqual(roots[item['legacyPath']], item['legacyGuid'])

    def test_renamed_packages_keep_dependencies_and_legacy_ids(self):
        expected = {
            'jp.pipipigiken.mpexporter': ['com.avatarnamecard.exporter'],
            'jp.pipipigiken.mpavatar-package': ['com.avatarnamecard.avatar-package'],
        }
        self.assertEqual(set(self.metadata['packages']), set(expected))
        for name, legacy in expected.items():
            manifest = self.metadata['packages'][name]
            self.assertEqual(manifest['legacyPackages'], legacy)
            self.assertNotIn('dependencies', manifest)
        exporter = self.metadata['packages']['jp.pipipigiken.mpexporter']
        self.assertEqual(exporter['vpmDependencies']['jp.pipipigiken.mpavatar-package'], self.metadata['version'])
        self.assertIn('com.vrchat.avatars', exporter['vpmDependencies'])
        self.assertIn('nadena.dev.ndmf', exporter['vpmDependencies'])

    def test_sync_of_new_ids_adds_exporter_dependencies_and_migration(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            source = root / 'source'
            destination = root / 'distribution'
            destination.mkdir()
            (destination / 'distribution.json').write_text(json.dumps(self.config))
            for name, relative in sync_from_app.SOURCES.items():
                package = source / relative
                package.mkdir(parents=True)
                manifest = {'name': name, 'version': '0.2.7', 'dependencies': {}, 'license': 'MIT'}
                (package / 'package.json').write_text(json.dumps(manifest))
                (package / 'LICENSE.txt').write_text('test license')
                (package / 'LICENSE_SCOPE.md').write_text('test scope')
            with patch.object(sync_from_app, 'ROOT', destination), patch('sys.argv', ['sync_from_app.py', '--source', str(source)]):
                sync_from_app.main()
            manifest = json.loads((destination / 'Packages/jp.pipipigiken.mpexporter/package.json').read_text())
            self.assertEqual(manifest['vpmDependencies']['jp.pipipigiken.mpavatar-package'], '0.2.7')
            self.assertIn('com.vrchat.avatars', manifest['vpmDependencies'])
            self.assertEqual(manifest['legacyPackages'], ['com.avatarnamecard.exporter'])
            self.assertNotIn('dependencies', manifest)
            self.assertEqual(manifest['license'], 'MIT')
            for name in sync_from_app.SOURCES:
                package = destination / 'Packages' / name
                self.assertEqual((package / 'LICENSE.txt').read_text(), 'test license')
                self.assertEqual((package / 'LICENSE_SCOPE.md').read_text(), 'test scope')

    def test_both_distribution_formats_include_full_mit_and_scope(self):
        unity_files = {}
        with tarfile.open(self.output / self.metadata['unitypackage']) as archive:
            for entry in archive.getmembers():
                if entry.name.endswith('/pathname'):
                    path = archive.extractfile(entry).read().decode()
                    if path.endswith(('/LICENSE.txt', '/LICENSE_SCOPE.md')):
                        unity_files[path] = archive.extractfile(entry.name.rsplit('/', 1)[0] + '/asset').read()
        self.assertEqual(len(unity_files), 4)
        for name, manifest in self.metadata['packages'].items():
            self.assertEqual(manifest['license'], 'MIT')
            root = self.config['packages'][name]['legacyPath']
            with zipfile.ZipFile(self.output / manifest['url'].rsplit('/', 1)[-1]) as archive:
                self.assertEqual(json.loads(archive.read('package.json'))['license'], 'MIT')
                for filename in ('LICENSE.txt', 'LICENSE_SCOPE.md'):
                    expected = (build_release.ROOT / 'Packages' / name / filename).read_bytes()
                    self.assertEqual(archive.read(filename), expected)
                    self.assertEqual(unity_files[root + '/' + filename], expected)
                license_text = archive.read('LICENSE.txt').decode()
                self.assertIn('Copyright (c) 2026 orange3134', license_text)
                self.assertIn('Permission is hereby granted, free of charge', license_text)
                self.assertIn('THE SOFTWARE IS PROVIDED "AS IS"', license_text)
                self.assertTrue(license_text.endswith('SOFTWARE.\n'))
                self.assertIn('does not place the exported .mpavatar', archive.read('LICENSE_SCOPE.md').decode())

    def test_missing_license_or_wrong_declaration_blocks_release(self):
        import shutil
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp) / 'repo'
            root.mkdir()
            shutil.copy(build_release.ROOT / 'distribution.json', root)
            shutil.copytree(build_release.ROOT / 'Packages', root / 'Packages')
            package = root / 'Packages/jp.pipipigiken.mpexporter'
            with patch.object(build_release, 'ROOT', root):
                for filename in ('LICENSE.txt', 'LICENSE_SCOPE.md'):
                    path = package / filename
                    original = path.read_bytes()
                    path.unlink()
                    with self.assertRaisesRegex(ValueError, 'Missing license document'):
                        build_release.build(root / 'output')
                    path.write_bytes(original)
                path = package / 'package.json'
                data = json.loads(path.read_text())
                data['license'] = 'UNLICENSED'
                path.write_text(json.dumps(data))
                with self.assertRaisesRegex(ValueError, 'Expected MIT'):
                    build_release.build(root / 'output')

    def test_output_is_reproducible(self):
        before = {p.name: p.read_bytes() for p in self.output.iterdir()}
        build_release.build(self.output)
        self.assertEqual(before, {p.name: p.read_bytes() for p in self.output.iterdir()})

    def test_wrong_tag_is_rejected(self):
        with self.assertRaises(ValueError):
            build_release.build(self.output, 'v999.0.0')



if __name__ == '__main__':
    unittest.main()
