# Changelog

## [Unreleased]
### Added

### Changed
- Removed the MipMap deletion feature

### Deprecated

### Removed

### Fixed

### Security

## [0.5.1] - 2025-12-02
### Changed
- Changed the TexTransTool dependency declaration to >=1.0.0.

### Fixed
- Fixed an issue where the alpha channel was not considered when changing the compression format of textures with TextureImporterType set to NormalMap.
- Fixed an issue where ParticleRenderer was mistakenly included as a processing target.
- Fixed an error that occurred when there were textures with unknown usage.

## [0.5.0] - 2025-04-23
### Added
- Added Japanese changelog.

### Changed
- Exclude materials operated on by TTT AtlasTexture from the execution target.
    - Textures modified by AtlasTexture should be handled within AtlasTexture or subsequent processes, so they have been excluded from the target.
- Changed the generation of TTT TextureConfigurator to absolute path mode instead of relative path.
    - Previously, relative path mode was used to retain references to textures that might be manipulated by TTT AtlasTexture.
    - However, this is no longer necessary because the above change excludes materials operated on by TTT AtlasTexture from the execution target.
    - This also corresponds to the removal of relative path mode in TexTransTool v0.10.0-beta.5.
- Significantly refactored the entire code in consideration of future feature additions.
    - There is no impact on execution results.
- Added `< v0.11.0` to the compatible TTT version specification.
- Changed the determination of whether a texture contains transparency from strict to allowing a certain value.
- Improved performance.

### Fixed
- Fixed a potential error that could occur when there are shaders other than lilToon or unregistered properties.
- Fixed an issue where the color space was not correctly reproduced when converting the format of gamma space textures.


## [0.4.1] - 2025-2-07
### Changed
- update vpmDependencies of TTT

## [0.4.0] - 2025-2-06
### Removed
- Optimize Material
    - This change may fix an issue where the preview RendererTexture was not displayed correctly in VirtualLens2.
    - Auto Configure Texture was searching for a target to apply to after this function has deleted textures that were not used.
    - so from now on, TexTransTool may display logs that do not have any applicable targets. This behavior is currently normal.

## [0.3.0] - 2025-1-25
### Added
- Specify textures to exclude from execution

### Fixed
- localization

## [0.2.3] - 2024-12-01
### Fixed
- incorrect changelogUrl

## [0.2.2] - 2024-12-01
### Added
- option to run on PC only

### Changed
- VpmDependencies of TexTransTool to >=0.8.7 < 0.9.0-beta.0
- By default it now only works on PC platform

## [0.2.1] - 2024-11-29
### Changed
- Improve build-time performance
- VpmDependencies of TexTransTool to v0.8.7
    - remove support for v0.9.0-beta.0
- Do not adjust texture format for Android or iOS

## [0.2.0] - 2024-11-16
### Added
- Format Mode of Optimize Texture Format
    - Conversion from DXT5 to BC7 improved the gradation expression while maintaining texture memory, but the download size increased slightly.
    - This is an addition of option to address this issue
    - No increase in texture memory or worsening in gradation expression from the default in any mode.
    - `LowDownloadSize:` No conversion that may increase the download size
    - `Balanced`: Only main textures that use 4 channels will be converted to DX7.
    - `HighQuality`: Convert to the highest quality format possible without increasing texture memory
- Optimize Material
    - Currently, the only feature is to delete unused properties and textures in lilToon.
    - Same as LI MaterialOptimizer, but is included due to the order of execution.
- Localization
- Option to maintain Crunch Compression

### Changed
- vpmDependencies of TexTransTool to v0.8.6 or v0.9.0-beta.0
- default effects due to the addition of Format Mode
    - gradation improvement is reduced, but download size is reduced
- improve shader check for lilToon

### Fixed
- incorrect value of resolution reduction of matcap
- increasing texture memory due to potential issue of TextureConfigurator by Optimize Material
- unnecessary logs when used in conjunction with LI MaterialOptimizer by Optimize Material
- Crunch Compression is not maintained

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
