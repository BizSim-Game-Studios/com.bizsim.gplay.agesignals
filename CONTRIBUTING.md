# Contributing

Thank you for your interest in contributing to this package!

## How to Contribute

### Bug Reports

1. Check [existing issues](../../issues) to avoid duplicates
2. Open a new issue with:
   - Package version and Unity version
   - Steps to reproduce
   - Expected vs actual behavior
   - Device/OS information (for Android-specific issues)

### Feature Requests

1. Open a [GitHub issue](../../issues/new) describing the feature
2. Explain the use case and expected behavior
3. If possible, reference relevant Google Play documentation

### Pull Requests

1. Fork the repository
2. Create a feature branch (`feature/your-feature-name`)
3. Follow existing code style and patterns
4. Test in Unity Editor and on Android device
5. Submit a PR with a clear description of changes

## Development Setup

1. Clone this repository
2. Add to your Unity project via `Packages/manifest.json`:
   ```json
   "com.bizsim.gplay.<package>": "file:../path/to/this/repo"
   ```
3. Open the Unity project and verify no compilation errors

## Code Style

- Follow existing naming conventions and patterns
- No code comments unless logic is non-obvious
- Keep changes minimal and focused
- Test on both Unity Editor (mock) and Android device

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE.md).
