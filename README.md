# Code Variable Renamer

## 📋 Описание

**Code Variable Renamer** — это мощный инструмент для автоматического переименования переменных в C# коде с использованием Microsoft Roslyn. Программа обеспечивает консистентное именование переменных по заданным правилам, идеально подходит для приведения кода к единому стилю в больших проектах.

## ✨ Особенности

### 🎯 Умное переименование по типу:
- **Private поля** → `_camelCase` с префиксом
- **Public поля** → `PascalCase` без префикса
- **Свойства** → `PascalCase`
- **Локальные переменные** → `camelCase`
- **Параметры методов** → `camelCase`

### 🛡️ Безопасные преобразования:
- Заменяет только отдельные слова (не затрагивает `maxStage` при замене `max` → `maximum`)
- Сохраняет аббревиатуры (ID, UI, URL, JSON, XML)
- Учитывает исключения через конфигурацию
- Поддерживает dry-run режим для предварительного просмотра

### 🌐 Поддерживаемые форматы:
- **camelCase** (по умолчанию)
- **PascalCase** 
- **snake_case**
- **SCREAMING_SNAKE** (UPPER_CASE)
- **kebab-case**

## 🚀 Быстрый старт

### 1. Сборка проекта

```bash
# Клонирование репозитория
git clone <repository-url>
cd CodeVariableRenamer

# Сборка
dotnet build

# Публикация как single-file приложение
dotnet publish -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=true -o ./publish
```

### 2. Базовое использование

```bash
# Переименовать переменные в одном файле
./publish/CodeVariableRenamer --file MyClass.cs --format camelCase

# Предварительный просмотр изменений
./publish/CodeVariableRenamer --file MyClass.cs --dry-run

# Обработать все файлы в директории
./publish/CodeVariableRenamer --dir ./Assets/Scripts --format PascalCase

# Использовать конфигурационный файл
./publish/CodeVariableRenamer --file Player.cs --config unity-config.json

# Показать справку
./publish/CodeVariableRenamer --help
```

## ⚙️ Параметры командной строки

| Параметр | Описание | Пример |
|----------|----------|---------|
| `--file` | Путь к файлу .cs | `--file MyClass.cs` |
| `--dir` | Директория для обработки | `--dir ./Scripts` |
| `--pattern` | Паттерн поиска файлов | `--pattern "*.cs"` |
| `--format` | Формат именования | `--format camelCase` |
| `--prefix` | Префикс для private полей | `--prefix "_"` |
| `--targets` | Цели для переименования | `--targets fields,properties` |
| `--dry-run` | Предпросмотр без изменений | `--dry-run` |
| `--config` | Конфигурационный файл JSON | `--config my-config.json` |
| `--help`, `-h` | Показать справку | `--help` |

## 🎮 Интеграция с VS Code

### 1. Настройка tasks.json

```json
{
    "version": "2.0.0",
    "tasks": [
        {
            "label": "Rename Variables (Current File)",
            "type": "shell",
            "command": "${workspaceFolder}/tools/CodeVariableRenamer",
            "args": [
                "--file",
                "${file}",
                "--format",
                "camelCase",
                "--prefix",
                "_"
            ],
            "group": "build",
            "presentation": {
                "echo": true,
                "reveal": "always",
                "focus": false,
                "panel": "shared",
                "clear": true
            }
        },
        {
            "label": "Preview Rename Changes",
            "type": "shell",
            "command": "${workspaceFolder}/tools/CodeVariableRenamer",
            "args": [
                "--file",
                "${file}",
                "--dry-run"
            ],
            "group": "build"
        },
        {
            "label": "Rename All Scripts",
            "type": "shell",
            "command": "${workspaceFolder}/tools/CodeVariableRenamer",
            "args": [
                "--dir",
                "${workspaceFolder}/Assets/Scripts",
                "--config",
                "${workspaceFolder}/unity-config.json"
            ],
            "group": "build"
        }
    ]
}
```

### 2. Горячие клавиши (.vscode/keybindings.json)

```json
[
    {
        "key": "ctrl+shift+r",
        "command": "workbench.action.tasks.runTask",
        "args": "Rename Variables (Current File)"
    },
    {
        "key": "ctrl+alt+r", 
        "command": "workbench.action.tasks.runTask",
        "args": "Preview Rename Changes"
    }
]
```

## 📁 Конфигурационные файлы

### Пример конфигурации для Unity:

```json
{
    "format": "camelCase",
    "prefix": "_",
    "targets": ["fields", "properties", "locals", "parameters"],
    
    "typeSpecificRules": {
        "fields": {
            "public": { "format": "PascalCase", "prefix": "" },
            "protected": { "format": "camelCase", "prefix": "" },
            "private": { "format": "camelCase", "prefix": "_" },
            "internal": { "format": "camelCase", "prefix": "" }
        },
        "properties": {
            "public": { "format": "PascalCase", "prefix": "" },
            "protected": { "format": "PascalCase", "prefix": "" },
            "private": { "format": "PascalCase", "prefix": "" }
        },
        "parameters": { "format": "camelCase", "prefix": "" },
        "locals": { "format": "camelCase", "prefix": "" }
    },
    
    "exceptions": [
        "ID", "UI", "URL", "JSON", "XML", "CPU", "GPU", "RAM",
        "MonoBehaviour", "GameObject", "Transform", "Vector3",
        "Awake", "Start", "Update", "FixedUpdate", "LateUpdate"
    ],
    
    "customReplacements": {
        "num": "number",
        "str": "string",
        "obj": "object",
        "btn": "button",
        "txt": "text",
        "go": "gameObject",
        "tf": "transform",
        "rb": "rigidbody",
        "col": "collider"
    }
}
```

## 📊 Пример работы

### Исходный код:
```csharp
public class PlayerController : MonoBehaviour
{
    [SerializeField] private int Player_HP = 100;
    private GameObject Player_obj;
    private bool player_is_alive = true;
    
    public void Update_HP(int hp_val)
    {
        int temp_hp = hp_val;
        this.Player_HP = temp_hp;
    }
}
```

### После обработки (`--format camelCase --prefix "_"`):
```csharp
public class PlayerController : MonoBehaviour
{
    [SerializeField] private int _playerHp = 100;
    private GameObject _playerObj;
    private bool _playerIsAlive = true;
    
    public void UpdateHp(int hpVal)
    {
        int tempHp = hpVal;
        this._playerHp = tempHp;
    }
}
```

## 🔧 Типоспецифичные правила

Программа позволяет задавать разные форматы для разных типов членов класса:

```json
"typeSpecificRules": {
    "fields": {
        "public": { "format": "PascalCase", "prefix": "" },
        "private": { "format": "camelCase", "prefix": "_" }
    },
    "properties": {
        "public": { "format": "PascalCase", "prefix": "" }
    }
}
```

## 🛠️ Компиляция для разных платформ

```bash
# macOS (Apple Silicon)
dotnet publish -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=true

# macOS (Intel)
dotnet publish -c Release -r osx-x64 --self-contained -p:PublishSingleFile=true

# Windows
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true

# Linux
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true
```

## 🐳 Docker поддержка

```dockerfile
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["VariableRenamer/VariableRenamer.csproj", "VariableRenamer/"]
RUN dotnet restore "VariableRenamer/VariableRenamer.csproj"
COPY . .
WORKDIR "/src/VariableRenamer"
RUN dotnet build "./VariableRenamer.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./VariableRenamer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "VariableRenamer.dll"]

```

## 🧪 Тестирование

```bash
# Создать тестовый файл
echo 'public class Test { private int test_var; public string PROP_NAME; }' > test.cs

# Протестировать
./publish/VariableRenamer --file test.cs --dry-run

# Проверить с конфигом
./publish/VariableRenamer --file test.cs --config test-config.json
```

## 🤝 Вклад в проект

1. Форкните репозиторий
2. Создайте ветку (`git checkout -b feature/improvement`)
3. Внесите изменения и протестируйте
4. Создайте Pull Request

## ⚠️ Ограничения и известные проблемы

- Не обрабатывает переменные внутри анонимных функций сложной структуры
- Может некорректно работать с динамическим кодом (`dynamic`)
- Частичные замены слов требуют аккуратной настройки (см. пример с `max` → `maximum`)

## 📞 Поддержка и обратная связь

Нашли ошибку или есть предложение?
1. Откройте Issue с примером проблемного кода
2. Укажите конфигурацию и параметры запуска
3. Приложите пример до/после обработки

## 🙏 Благодарности

- **Microsoft Roslyn** за мощный API для анализа кода
- Сообщество .NET за лучшие практики и стандарты
- Unity Technologies за вдохновляющие примеры кода

---

**Пусть ваш код будет чистым и консистентным!** ✨