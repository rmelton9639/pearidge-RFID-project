# Changelog

All notable changes to the Pea Ridge RFID Project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-10-08

### Added - Initial Production Release

#### Raspberry Pi Component
- Multi-zone RFID reader with M6E Nano support
- 4-zone antenna multiplexing per Raspberry Pi
- TCP server for real-time tag broadcasting (JSON format)
- Automatic RF power management (27 dBm)
- Serial communication with M6E via UART
- Zone cycling algorithm for continuous monitoring
- Auto-reconnect for TCP clients
- Systemd service integration for auto-start
- Configuration via environment variables
- Comprehensive logging to `/opt/pearidge-rfid/logs/`

#### Lobby Computer Application
- C# Windows Forms application
- Simultaneous connection to 8 zones (2 Raspberry Pi units)
- Real-time connection monitoring and status display
- Color-coded zone visualization:
  - Zone 1 (Bathing): Light Blue
  - Zone 2 (Drying): Light Coral
  - Zone 3 (Grooming Room 1): Light Green
  - Zone 4 (Grooming Room 2): Light Yellow
  - Zone 5 (Entryway): Plum
  - Zone 6 (Waiting Kennels): Peach Puff
  - Zone 7 (Play Yard): Light Cyan
  - Zone 8 (Pickup Kennel): Light Salmon
- Customer queue window with zone tracking
- Automatic zone movement detection and updates
- Visual flash animation when dogs change zones
- Tag popup notifications with customer information
- Audio alerts (ding sound) on tag detection
- Zone history viewer with filtering
- CSV export for reporting
- Customer database management
- Add/edit tag functionality
- System tray minimization
- Auto-reconnect on connection loss
- Statistics dashboard (connected zones, tags read, active customers)

#### Database Component
- MySQL database schema (rfid_database)
- `rfid_tags` table for customer information
- `tag_reads` table for complete zone history
- Pre-built views:
  - `latest_tag_locations` - Current zone per tag
  - `current_dogs_in_facility` - Dogs in facility (last 8 hours)
  - `zone_activity_summary` - Daily zone statistics
- Stored procedures:
  - `GetDogMovementHistory` - Individual dog path tracking
  - `GetZoneStatistics` - Zone analytics by date range
- Indexes for optimal query performance
- Support for automatic backups

#### Documentation
- Complete deployment guide for lobby computer
- Quick reference card for daily operations
- System architecture diagrams
- Raspberry Pi setup instructions
- Troubleshooting guides
- Contributing guidelines
- MIT license

#### Hardware Support
- Raspberry Pi 3B+ (Debian Bookworm)
- ThingMagic M6E Nano RFID readers
- 12" x 12" panel antennas (10.5 dBi gain)
- UHF RFID frequency range: 902-928 MHz (NA region)
- Gen2/ISO 18000-6C protocol support

#### Network & Integration
- TCP socket communication (ports 5001-5008)
- JSON message format for tag data
- Support for multiple simultaneous clients
- Local network operation (no internet required)
- Static IP configuration for Raspberry Pi units

### Configuration
- Environment-based configuration for Raspberry Pi
- Connection string configuration for lobby computer
- Configurable zone colors
- Adjustable tag read cooldown periods
- Customizable antenna multiplexing timing

### Performance
- Tag read rate: 50+ tags/second per reader
- Response time: <100ms from tag detection to display
- Database capacity: Millions of reads
- Network bandwidth: <1 Mbps
- Supports 8 simultaneous zone connections

### Security
- MySQL user authentication
- Local network isolation
- No external internet dependencies
- Password-protected database access

## [Unreleased]

### Planned Features
- Email notifications for pickup ready
- SMS integration for customer alerts
- Mobile app for staff monitoring
- Advanced analytics and reporting
- Tag battery level monitoring
- Multi-facility support
- Cloud backup integration
- API for third-party integrations

### Under Consideration
- Barcode scanner integration
- QR code check-in system
- Appointment scheduling integration
- Payment system integration
- Customer self-check-in kiosk
- Automated photo capture per zone
- Temperature monitoring integration

---

## Version History Key

- **Added** - New features
- **Changed** - Changes to existing functionality
- **Deprecated** - Soon-to-be removed features
- **Removed** - Removed features
- **Fixed** - Bug fixes
- **Security** - Security vulnerability fixes

---

[1.0.0]: https://github.com/yourorg/pearidge-RFID-project/releases/tag/v1.0.0
