## 🛠️ 開発ツール (Development Tools)

このリポジトリでは、開発を効率化するためのカスタム .NET ローカルツールを提供しています。
リポジトリをクローンした後、最初に一度だけ以下のコマンドを実行して、これらのツールをインストールしてください。

```shell
dotnet tool restore
```

### LambdaTools (`dotnet new-lambda`)

新しいLambda関数のプロジェクト雛形を、定義されたディレクトリ構成に従って自動生成するツールです。

#### 使い方

プロジェクトのルートディレクトリで、以下のコマンドを実行します。

* **シンプルな構成のLambdaを作成する場合:**
    ```shell
    dotnet new-lambda --name MySimpleBatch --type simple
    ```

* **DDD構成の複雑なLambdaを作成する場合:**
    ```shell
    dotnet new-lambda --name MyComplexBatch --type ddd
    ```

**➡️ [ツールの全オプションや詳細な使い方はこちら](./tools/LambdaTools/README.md)**
