# Contributing to Pea Ridge RFID Project

Thank you for your interest in contributing to the Pea Ridge RFID tracking system!

## 🎯 Project Goals

This system is designed to:
- Track dogs through grooming operations safely and efficiently
- Provide real-time location updates to staff
- Log complete history for operational analysis
- Be reliable, maintainable, and easy to use

## 📋 Before Contributing

1. **Understand the system** - Review the documentation in `/docs`
2. **Test environment** - Set up a development/test system
3. **Discuss major changes** - Open an issue before significant work

## 🔧 Development Setup

### Raspberry Pi Development

```bash
# Clone repository
git clone https://github.com/yourorg/pearidge-RFID-project.git
cd pearidge-RFID-project/raspberry-pi

# Install dependencies
pip3 install -r requirements.txt

# Run tests (if available)
python3 -m pytest tests/
```

### Lobby Computer Development

```bash
# Open in Visual Studio
# Build solution
# Run tests
```

## 📝 Coding Standards

### Python (Raspberry Pi)

- Follow PEP 8 style guide
- Use type hints where appropriate
- Document all functions with docstrings
- Keep functions small and focused

**Example:**
```python
def read_from_zone(ser: serial.Serial, zone_num: int) -> Optional[dict]:
    """
    Read RFID tags from specific zone.
    
    Args:
        ser: Serial port connection to M6E module
        zone_num: Zone number (1-4)
        
    Returns:
        Dictionary with tag data if found, None otherwise
    """
    # Implementation...
```

### C# (Lobby Computer)

- Follow Microsoft C# coding conventions
- Use meaningful variable names
- Comment complex logic
- Handle exceptions appropriately

**Example:**
```csharp
/// <summary>
/// Connects to specified RFID zone
/// </summary>
/// <param name="zone">Zone configuration object</param>
private async void ConnectToZone(ZoneConnection zone)
{
    // Implementation...
}
```

## 🐛 Bug Reports

**Before submitting:**
1. Check if the issue already exists
2. Test on latest version
3. Gather system information

**Include in report:**
- Clear description of the problem
- Steps to reproduce
- Expected vs actual behavior
- System information (OS, versions, hardware)
- Log files (sanitize sensitive data!)
- Screenshots if applicable

**Example:**
```markdown
### Bug Description
Zone 3 shows "Disconnected" but Raspberry Pi service is running

### Steps to Reproduce
1. Start lobby application
2. Click "Connect All Zones"
3. Zones 1, 2, 4 connect successfully
4. Zone 3 remains disconnected

### Environment
- Windows 10 Pro x64
- Application Version: 1.0.0
- pr-rpi001: Raspberry Pi OS Bookworm
- Network: 192.168.1.0/24

### Logs
```
[Error log content here]
```
```

## ✨ Feature Requests

**Before requesting:**
1. Check if feature already planned
2. Consider if it fits project scope
3. Think about implementation impact

**Include in request:**
- Clear description of feature
- Use case / business justification
- Potential implementation approach
- Alternative solutions considered

## 🔀 Pull Request Process

### 1. Fork & Branch

```bash
# Fork the repository on GitHub
# Clone your fork
git clone https://github.com/YOUR_USERNAME/pearidge-RFID-project.git

# Create feature branch
git checkout -b feature/amazing-feature
```

### 2. Make Changes

- Keep commits focused and atomic
- Write clear commit messages
- Test thoroughly

**Good commit message:**
```
Add zone reconnection timeout handling

- Implement 30-second timeout for zone connections
- Add retry logic with exponential backoff
- Update status display to show reconnection attempts

Fixes #123
```

### 3. Test

- Test on actual hardware if possible
- Verify no existing functionality broken
- Check for memory leaks or performance issues
- Test error conditions

### 4. Document

- Update relevant documentation
- Add comments for complex logic
- Update CHANGELOG.md if applicable

### 5. Submit Pull Request

**Title:** Clear, descriptive summary

**Description:**
```markdown
## Changes
- Feature X added
- Bug Y fixed
- Documentation updated

## Testing
- Tested on Raspberry Pi 3B+
- Tested with 4 zones
- Verified database logging
- Checked memory usage

## Screenshots
[If applicable]

## Checklist
- [ ] Code tested
- [ ] Documentation updated
- [ ] No breaking changes
- [ ] Follows coding standards
```

### 6. Code Review

- Respond to feedback constructively
- Make requested changes
- Re-test after modifications

## 📦 Release Process

1. Version bump in relevant files
2. Update CHANGELOG.md
3. Tag release: `git tag -a v1.1.0 -m "Release v1.1.0"`
4. Push tags: `git push --tags`
5. Create GitHub release with notes

## 🚫 What NOT to Contribute

- Unnecessary dependencies
- Breaking changes without discussion
- Code that violates security best practices
- Untested features
- Incomplete implementations
- Code with hardcoded credentials/secrets

## ✅ Good First Issues

Looking to contribute but not sure where to start?

- Documentation improvements
- Bug fixes
- Unit test additions
- Error message improvements
- UI/UX enhancements
- Performance optimizations

## 🤝 Code of Conduct

### Our Standards

- Be respectful and inclusive
- Accept constructive criticism
- Focus on what's best for the project
- Show empathy towards others

### Unacceptable Behavior

- Harassment or discriminatory comments
- Personal attacks
- Trolling or inflammatory comments
- Publishing others' private information

## 📞 Questions?

- Open an issue for discussion
- Email: dev@pearidge.com
- Check documentation first

## 🙏 Thank You!

Your contributions help make grooming operations safer and more efficient for dogs and staff alike!

---

**Remember:** This is a production system used in a real business. Quality and reliability are paramount.
