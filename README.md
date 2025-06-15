## 🛠️ 開発ツール (Development Tools)

このリポジトリでは、開発を効率化するためのカスタム .NET ローカルツールを提供しています。
リポジトリをクローンした後、最初に一度だけ以下のコマンドを実行して、これらのツールをインストールしてください。

```shell
dotnet tool restore
```

### LambdaTools (`dotnet forge`)

新しいLambda関数のプロジェクト雛形を、定義されたディレクトリ構成に従って自動生成するツールです。

#### 使い方

プロジェクトのルートディレクトリで、以下のコマンドを実行します。

* **シンプルな構成のLambdaを作成する場合:**
    ```shell
    dotnet forge function --name MySimpleBatch --type simple
    ```

* **DDD構成の複雑なLambdaを作成する場合:**
    ```shell
    dotnet forge function --name MyComplexBatch --type ddd
    ```
* **共有ライブラリを生成する場合:**
    ```shell
    dotnet forge shared --name MySharedLibrary
    ```

**➡️ [ツールの全オプションや詳細な使い方はこちら](./tools/LambdaTools/README.md)**

## 📁 ディレクトリ構成 (Directory Structure)

このリポジトリは、以下の思想に基づいたモノレポ構成を採用しています。

- **機能別カプセル化**: 各Lambdaは、`functions/`配下で自己完結したコンポーネントとして管理されます。
- **関心の分離**: アプリケーションコード (`functions/`, `shared/`)、インフラ定義 (`terraform/`)、開発ツール (`tools/`) の責務を明確に分離します。

```text
lambda-scaffold-dotnet/
│
├── .config/                # .NET ローカルツールの設定 (`dotnet tool restore`で利用)
├── functions/              # ✅ 主役: 各Lambda関数を機能単位で格納する場所
│   ├── MyComplexBatch/     # (例: DDDを適用した複雑なバッチ)
│   └── MySimpleBatch/      # (例: シンプルな構成のバッチ)
│
├── scripts/                # CI/CDなどで利用するヘルパースクリプト
│
├── shared/                 # 複数のLambdaで共有する共通ライブラリ (Shared Kernelなど)
│
├── terraform/              # インフラ定義 (IaC)。(例: Terraform)
│
├── tools/                  # 開発を補助する.NET ローカルツール (`dotnet forge`)
│
└── YourSolutionName.sln    # 全プロジェクトを管理するソリューションファイル
```

### 🧬 Functionの構成パターン (Function Structure Patterns)

このリポジトリでは、Lambda関数が担う責務の複雑さに応じて、2つの主要なプロジェクト構成パターンを用意しています。
`dotnet forge` ツールは、これらの雛形を自動で生成することができます。

#### 1. シンプルな構成 (Simple Pattern)

単純なデータ変換や、ビジネスロジックがほとんどないバッチ処理に適しています。
プロジェクトの数が最小限で、見通しが良いのが特徴です。

**生成コマンド:**
```shell
dotnet forge function --name MySimpleBatch --type simple
```
or
```shell
dotnet forge function --name MySimpleBatch
```


**生成されるディレクトリ構成:**
```Plaintext
functions/
└── MySimpleBatch/
    ├── src/
    │   ├── Function.cs
    │   └── MySimpleBatch.Lambda.csproj
    └── test/
        ├── FunctionTest.cs
        └── MySimpleBatch.Lambda.Tests.csproj
```

#### 2. DDD構成 (DDD Pattern)

複雑なビジネスルールや状態管理を伴う、重要なバッチ処理に適しています。
関心の分離が徹底されており、テスト容易性と長期的なメンテナンス性に優れています。

**生成コマンド:**
```shell
dotnet forge function --name MyComplexBatch --type ddd
```

**生成されるディレクトリ構成:**
```Plaintext
functions/
└── MyComplexBatch/
    ├── src/
    │   ├── MyComplexBatch.Application/
    │   │   ├── Function.cs
    │   │   └── MyComplexBatch.Application.csproj
    │   ├── MyComplexBatch.Domain/
    │   │   └── MyComplexBatch.Domain.csproj
    │   └── MyComplexBatch.Infrastructure/
    │       └── MyComplexBatch.Infrastructure.csproj
    └── test/
        ├── MyComplexBatch.Application.Tests/
        │   └── MyComplexBatch.Application.Tests.csproj
        └── MyComplexBatch.Domain.Tests/
            └── MyComplexBatch.Domain.Tests.csproj
```

#### 3. 共有ライブラリの作成
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

---
新しいLambdaを追加する際は、その責務の複雑さに応じて、これらのパターンから適切なものを選択してください。
