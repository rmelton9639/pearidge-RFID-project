# 🚀 Quick Start Guide - Pea Ridge RFID Project

Get up and running in minutes!

## Prerequisites Check

Before you begin, ensure you have:

**For Raspberry Pi:**
- [ ] Raspberry Pi 3B+ with Debian Bookworm
- [ ] M6E Nano RFID reader module
- [ ] 4x RFID antennas (12" panel, 10.5 dBi)
- [ ] Network connectivity
- [ ] Python 3.11+

**For Lobby Computer:**
- [ ] Windows 10/11 PC
- [ ] Visual Studio 2022 (or .NET 4.7.2+)
- [ ] MySQL Server 8.0+
- [ ] Network connectivity to Pi units

## 🏃 Quick Installation

### Option A: Raspberry Pi (5 minutes)

```bash
# 1. Clone repository
git clone https://github.com/yourorg/pearidge-RFID-project.git
cd pearidge-RFID-project/raspberry-pi/scripts

# 2. Install dependencies
sudo apt update
sudo apt install -y python3-pip python3-serial
pip3 install pyserial --break-system-packages

# 3. Copy script to system location
sudo mkdir -p /opt/pearidge-rfid/logs
sudo cp multi_zone_rfid.py /opt/pearidge-rfid/
sudo chmod +x /opt/pearidge-rfid/multi_zone_rfid.py

# 4. Test run (Ctrl+C to stop)
python3 /opt/pearidge-rfid/multi_zone_rfid.py

# 5. Setup as service (optional)
# See raspberry-pi/docs/ for systemd setup
```

### Option B: Lobby Computer (10 minutes)

```bash
# 1. Clone repository
git clone https://github.com/yourorg/pearidge-RFID-project.git
cd pearidge-RFID-project/lobby-computer

# 2. Setup MySQL database
mysql -u root -p < database/PEARIDGE_DATABASE_SETUP.sql

# 3. Open in Visual Studio
# - Create new Windows Forms project named "RFIDLookupApp"
# - Add all files from src/ folder
# - Install NuGet packages:
#   - MySql.Data (v8.0.33)
#   - Newtonsoft.Json (v13.0.3)

# 4. Update connection string in MainForm.cs
# Find line:
#   private const string connectionString = "Server=localhost;..."
# Update with your MySQL password

# 5. Build and Run (F5)
```

## ⚡ Super Quick Test

Want to test immediately without full setup?

### Test Raspberry Pi Script:

```bash
# Just run the script directly
python3 raspberry-pi/scripts/multi_zone_rfid.py

# You should see:
# ✓ Connected to M6E on /dev/ttyAMA0
# ✓ TCP server listening on 0.0.0.0:5002
# ✓ M6E initialized
```

### Test Lobby Application:

```bash
# Build the solution
msbuild lobby-computer/src/RFIDLookupApp.sln /t:Build /p:Configuration=Release

# Or just open in Visual Studio and hit F5
```

## 🎯 First Run Checklist

After installation, verify:

### Raspberry Pi:
- [ ] Script starts without errors
- [ ] TCP port is listening (check with `netstat -tuln | grep 5002`)
- [ ] M6E module is responding
- [ ] Antennas are connected

### Lobby Computer:
- [ ] MySQL connection succeeds
- [ ] Application shows 8 zones
- [ ] "Connect All Zones" button works
- [ ] Customer Queue window opens

## 🐛 Common First-Time Issues

**Pi: "No module named 'serial'"**
```bash
pip3 install pyserial --break-system-packages
```

**Pi: "Permission denied /dev/ttyAMA0"**
```bash
sudo usermod -a -G dialout $USER
# Log out and back in
```

**Lobby: "Cannot connect to MySQL"**
```
1. Start MySQL: net start MySQL80
2. Check password in connection string
3. Verify database exists: SHOW DATABASES;
```

**Lobby: "Assembly not found"**
```
Install NuGet packages:
- MySql.Data
- Newtonsoft.Json
```

## 📚 Next Steps

Once running:

1. **Read the docs:**
   - `lobby-computer/docs/PEARIDGE_LOBBY_DEPLOYMENT_GUIDE.txt`
   - `lobby-computer/docs/PEARIDGE_QUICK_REFERENCE.txt`

2. **Configure zones:**
   - Update Pi hostnames and IPs
   - Set zone names in code if needed

3. **Add customers:**
   - Use "Add Tag" button in lobby app
   - Import customer list to database

4. **Setup auto-start:**
   - Configure systemd on Pi
   - Add to Windows startup

## 🆘 Need Help?

- Check `docs/SYSTEM_ARCHITECTURE.txt` for system overview
- Review `TROUBLESHOOTING.md` (if available)
- Open an issue on GitHub
- Email: support@pearidge.com

## 🎉 Success Indicators

You'll know it's working when:

✅ All 8 zones show "Connected ✓" (green)
✅ Customer queue window is open
✅ Tag detection causes a "ding" sound
✅ Customer appears in queue with zone info
✅ Zone colors display correctly

---

**Ready to start tracking! 🐕**

For detailed information, see the main [README.md](README.md)
