# Changelog

## [Unreleased]
### Added

### Changed
- MipMapの削除機能を削除

### Deprecated

### Removed

### Fixed

### Security

## [0.5.1] - 2025-12-02
### Changed
- TexTransToolの依存関係の宣言を>=1.0.0に変更。

### Fixed
- TextureImporterTypeがNormalMapのテクスチャの圧縮形式を変更する際に、アルファが考慮されていない問題を修正。
- ParticleRendererが誤って処理の対象となっていた問題を修正。
- 不明な使用用途を持つテクスチャが存在する際のエラーを修正

## [0.5.0] - 2025-04-23
### Added
- 日本語の変更履歴を追加。

### Changed
- TTT AtlasTextureで操作されるマテリアルを、実行対象から除外するように変更。
    - AtlasTexture で変更されるテクスチャは、AtlasTexture 内部またはそれ以降の処理で操作されるべきであるため、対象から除外しました。
- TTT TextureConfigurator の生成を、相対パスではなく絶対パスモードで行うように変更。
    - これまで、TTT AtlasTexture で操作される可能性のあるテクスチャへの参照を保持するため、相対パスモードを使用していました。
    - しかし、上記の変更によりTTT AtlasTextureで操作されるマテリアルを実行対象から除外するように変更したため、不要となりました。
    - これは同時に、TexTransTool v0.10.0-beta.5 における相対パスモードの削除に対応するものです。
- 今後の機能追加を考慮し、コード全体を大幅にリファクタリング。
    - 実行結果に影響はありません。
- 互換性のある TTT のバージョン指定に、`< v0.11.0` を追加。
- テクスチャに透明度が含まれているかの判定を、厳密なものから一定の値を許容するものに変更。
- パフォーマンスを改善。

### Fixed
- lilToon 以外のシェーダーや、登録されていないプロパティが存在する場合に発生する可能性のあるエラーを修正。
- ガンマ空間のテクスチャのフォーマットを変換する際に、色空間が正しく再現されない問題を修正。


## 以下のバージョンには日本語の変更履歴はありません。

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
