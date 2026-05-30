# Agent Automation

Общий source of truth для Skills и MCP в AdaptiveSpritesDMItool. Этот файл предназначен одновременно для Codex, Zoo/Roo Code и совместимых MCP-клиентов.

## Зачем это нужно
- Ускорить вход AI-агентов в контекст проекта без повторного чтения всего репозитория.
- Направить агента к правильному workflow для WPF/MVVM, DMI pipeline, verification/release и agent config задач.
- Дать управляемый набор MCP для актуальной документации, GitHub/CI context и repo-local файлов без хранения секретов в git.
- Сохранить один общий смысловой слой в `AGENTS/**`; `.codex/**` и `.roo/**` остаются локальными пользовательскими настройками и не уходят в репозиторий.

## Кому предназначено
- Владельцу проекта при работе через Codex и Zoo/Roo Code.
- Будущим AI-агентам, которые заходят в репозиторий и должны соблюдать project overlay.
- Совместимым MCP-клиентам, которым нужен безопасный набор серверов и границы доступа.

## Общие Skills
- `adaptive-sprites-dmi-tool`: project overlay bridge; всегда ведет к `AGENTS.md` и `AGENTS/*.md`.
- `adaptive-wpf-mvvm-workflow`: XAML, `App.xaml.cs`, DI, navigation, commands, bindings, dialogs, WPF-UI, shell state.
- `adaptive-dmi-processing-workflow`: `.dmi`, DMISharp, JSON config, CSV import, preview, batch, path/format contracts.
- `adaptive-verification-release`: build/test, hidden Unicode check, GitHub Actions parity, release package validation.
- `adaptive-agent-config-workflow`: `.codex`, `.roo`, prompts, skills, MCP, `.gitignore`, onboarding rules.

Codex может локально хранить discoverable wrappers в `.codex/skills/**`. Zoo/Roo может локально хранить wrappers в `.roo/skills/**`. Эти папки игнорируются git; переносимый source of truth должен оставаться здесь, в `AGENTS/**`, `README*` или `docs/**`.

## MCP catalog
- `context7`: базовый docs MCP для актуальных API/framework docs. Разрешен в runtime configs.
- `github`: optional MCP для PR/issues/Actions/release context. Требует `GITHUB_PERSONAL_ACCESS_TOKEN` только из окружения. По умолчанию использовать read-first.
- `filesystem`: optional MCP для клиентов, которым нужен MCP-доступ к файлам. Allowlist только на корень `D:/GitHub/AdaptiveSpritesDMItool`.

Не включать browser/database/cloud/deploy/production MCP в базовый профиль проекта.

## MCP snippets

Codex TOML snippet:

```toml
[mcp_servers.context7]
command = "cmd"
args = ["/c", "npx", "-y", "@upstash/context7-mcp@latest"]
startup_timeout_sec = 20
tool_timeout_sec = 120

[mcp_servers.github]
command = "docker"
args = [
  "run",
  "-i",
  "--rm",
  "-e",
  "GITHUB_PERSONAL_ACCESS_TOKEN",
  "ghcr.io/github/github-mcp-server"
]
startup_timeout_sec = 20
tool_timeout_sec = 120

[mcp_servers.filesystem]
command = "cmd"
args = [
  "/c",
  "npx",
  "-y",
  "@modelcontextprotocol/server-filesystem",
  "D:/GitHub/AdaptiveSpritesDMItool"
]
startup_timeout_sec = 20
tool_timeout_sec = 120
```

Zoo/Roo JSON snippet:

```json
{
  "mcpServers": {
    "context7": {
      "command": "cmd",
      "args": ["/c", "npx", "-y", "@upstash/context7-mcp@latest"]
    },
    "github": {
      "command": "docker",
      "args": [
        "run",
        "-i",
        "--rm",
        "-e",
        "GITHUB_PERSONAL_ACCESS_TOKEN",
        "ghcr.io/github/github-mcp-server"
      ]
    },
    "filesystem": {
      "command": "cmd",
      "args": [
        "/c",
        "npx",
        "-y",
        "@modelcontextprotocol/server-filesystem",
        "D:/GitHub/AdaptiveSpritesDMItool"
      ]
    }
  }
}
```

## Safety
- Не хранить токены, OAuth/session files, `.env`, private keys или credentials в tracked файлах.
- Не расширять filesystem allowlist за пределы репозитория без явного scope.
- Если MCP output противоречит локальному source of truth, проверить факт по `AGENTS/**`, `docs/**`, коду и тестам.
- Если MCP или tool call попал в цикл ошибок, остановиться, сузить задачу до одного файла или одной команды и сообщить блокер.
