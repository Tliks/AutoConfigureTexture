# Changelog

## [Unreleased]
### Added

### Changed

### Deprecated

### Removed

### Fixed

### Security

## [0.1.2] - 2024-11-10
### Changed
- vpmDependencies of TexTransTool to be limited to v0.8.4 or v0.9.0-beta.0
    - This package is unstable because it uses properties and methods of TexTransTool that are not API-compliant
    - Generally, only versions that have been confirmed to work will be allowed, but this may be relaxed in the future.

### Fixed
- Target texture is lost when used in conjunction with AtlasTexture etc.
    - The selector mode of generated TextureConfigurator is not appropriate.
    - changed selector mode from Absolute to Relative
- Checking for an already existing TextureConfigurator is inaccurate
- The changelogUrl at package.json is a broken link

## [0.1.1] - 2024-11-08
### Fixed
- 2nd/3rd textures are converted to DX1 even though they used transparency.

## [0.1.0] - 2024-11-05
### Added
- initial release
