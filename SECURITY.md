# Security Policy

## Reporting a Vulnerability

If you discover a security vulnerability in FocalFade, please report it responsibly:

1. **Do not** open a public GitHub issue for security vulnerabilities
2. Email the maintainers directly (see README for contact)
3. Include a description of the vulnerability and steps to reproduce

## Security Design

FocalFade is designed with security in mind:

- **No network calls** - FocalFade does not make any outbound network connections at runtime
- **No telemetry** - zero data collection or reporting
- **Local-only storage** - settings and logs are stored only on the local machine
- **No elevation required** - runs as the current user (asInvoker), does not require administrator privileges
- **No installer** - portable executable, no system-wide changes
- **Registry usage** - only writes to HKCU\Software\Microsoft\Windows\CurrentVersion\Run for optional auto-start

## Scope

The following are in scope for security review:
- Settings file parsing (JSON deserialization)
- Registry operations (startup registration)
- Win32 API calls (P/Invoke)
- File system operations (settings/logs)

The following are out of scope:
- The overlay rendering itself (purely visual, no data processing)
- The tray icon UI
