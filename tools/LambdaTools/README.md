# LambdaTools (`dotnet forge`) 取扱説明書

## 1. 概要

`dotnet forge` は、このリポジトリで利用するカスタム .NET ローカルツールです。
新しいAWS Lambda関数のプロジェクト雛形を、定義された標準ディレクトリ構成に従って自動生成します。

このツールを利用することで、誰が作成しても一貫性のあるプロジェクト構造が保証され、手作業による設定ミスを防ぎ、開発の初期セットアップを迅速化します。

## 2. インストール

このツールは、.NETのローカルツールとして管理されています。
リポジトリをクローンした後、最初に一度だけ以下のコマンドを**リポジトリのルートディレクトリ**で実行し、ツールをインストールしてください。

```shell
dotnet tool restore
```

## 3. 使い方

### 基本コマンド

`dotnet forge <サブコマンド> [オプション]`

### サブコマンド
`function`

新しいLambda関数プロジェクトを生成します。
- オプション
  - `--name, -n <NAME>` (必須): 生成するLambdaの機能名。
  - `--type, -t <TYPE>` (任意): テンプレート種類 (`simple`または`ddd`)。デフォルトは`simple`

`shared`
複数のLambdaで利用する共有ライブラリプロジェクトを生成します。
- オプション:
  - `--name, -n <NAME>` (必須): 生成する共有ライブラリ名。

## 4. 使用例

### 例1：シンプルなLambdaの作成

単純なデータ処理など、ビジネスロジックが複雑でないLambdaを作成します。

**コマンド:**

```shell
dotnet forge function --name MySimpleBatch --type simple
```

**生成される構成:**

```markdown
functions/
└── MySimpleBatch/
    ├── src/
    │   ├── Function.cs
    │   └── MySimpleBatch.Lambda.csproj
    └── test/
        ├── FunctionTest.cs
        └── MySimpleBatch.Lambda.Tests.csproj
```

### 例2：DDD構成のLambdaの作成

複雑なビジネスロジックを含む、重要なバッチ処理などのLambdaを作成します。

**コマンド:**

```shell
dotnet forge function --name MyComplexBatch --type ddd
```

**生成される構成:**

```markdown
functions/
└── MyComplexBatch/
    ├── src/
    │   ├── MyComplexBatch.Application/
    │   ├── MyComplexBatch.Domain/
    │   └── MyComplexBatch.Infrastructure/
    └── test/
        ├── MyComplexBatch.Application.Tests/
        └── MyComplexBatch.Domain.Tests/
```

### 例3:共有ライブラリの作成
    ```shell
    dotnet forge shared --name MySharedLibrary
    ```

**生成されるディレクトリ構成:**
```Plaintext
shared/
└── MySharedLibrary/
    ├── src/
    │   └── MySharedLibrary.csproj
    └── test/
        └── MySharedLibrary.Tests.csproj
```

## 5. トラブルシューティング

#### エラー: `command not found`
- **原因**: ツールが正しくインストールされていません。
- **解決策**: リポジトリのルートで `dotnet tool restore` を実行してください。

#### エラー: `Could not find the repository root`
- **原因**: コマンドを実行しているディレクトリが、Gitリポジトリとして初期化されていません。
- **解決策**: プロジェクトのルートディレクトリで `git init` を実行してください。

## 6. 開発者向け情報 (ツール自体の改修)

このツール自体のソースコード (`Program.cs`) を修正した場合、その変更を反映させるにはツールの更新が必要です。
開発中は、以下の「アンインストール＆再インストール」の手順が最も確実です。

```shell
# 1. パッケージの再作成
dotnet pack ./tools/LambdaTools/

# 2. 既存ツールのアンインストール
dotnet tool uninstall LambdaTools

# 3. 新しいツールの再インストール
dotnet tool install --add-source ./tools/LambdaTools/bin/Release --version 1.0.0 LambdaTools
```
