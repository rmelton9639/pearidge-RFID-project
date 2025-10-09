using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace RFIDLookupApp
{
    public partial class MainForm : Form
    {
        private const string connectionString = "Server=localhost;Database=rfid_database;Uid=root;Pwd=Sc0ttyM3lt0n!;";

        // Multi-zone reader configuration
        private Dictionary<string, ZoneConnection> zoneConnections;
        private CustomerQueueForm queueForm;

        // UI Controls
        private ListView zoneStatusList;
        private Button btnConnectAll;
        private Button btnDisconnectAll;
        private Label lblGlobalStatus;
        private Button btnViewDatabase;
        private Button btnViewZoneHistory;
        private Button btnAddTag;
        private Button btnMinimize;
        private Button btnShowQueue;
        private NotifyIcon notifyIcon;

        public MainForm()
        {
            InitializeComponent();
            SetupDatabase();
            SetupNotifyIcon();
            InitializeZones();
            queueForm = new CustomerQueueForm(connectionString);
        }

        private void InitializeZones()
        {
            // Define all 8 Pea Ridge zones
            zoneConnections = new Dictionary<string, ZoneConnection>
            {
                // Raspberry Pi #1 - Grooming Operations (192.168.1.201)
                { "Bathing", new ZoneConnection
                    {
                        ZoneName = "Bathing",
                        ZoneNumber = 1,
                        PiId = "pr-rpi001",
                        IpAddress = "192.168.1.201",
                        Port = 5001,
                        Color = Color.LightBlue
                    }
                },
                { "Drying", new ZoneConnection
                    {
                        ZoneName = "Drying",
                        ZoneNumber = 2,
                        PiId = "pr-rpi001",
                        IpAddress = "192.168.1.201",
                        Port = 5002,
                        Color = Color.LightCoral
                    }
                },
                { "Grooming Room 1", new ZoneConnection
                    {
                        ZoneName = "Grooming Room 1",
                        ZoneNumber = 3,
                        PiId = "pr-rpi001",
                        IpAddress = "192.168.1.201",
                        Port = 5003,
                        Color = Color.LightGreen
                    }
                },
                { "Grooming Room 2", new ZoneConnection
                    {
                        ZoneName = "Grooming Room 2",
                        ZoneNumber = 4,
                        PiId = "pr-rpi001",
                        IpAddress = "192.168.1.201",
                        Port = 5004,
                        Color = Color.LightGoldenrodYellow
                    }
                },
                
                // Raspberry Pi #2 - Customer & Holding (192.168.1.202)
                { "Entryway", new ZoneConnection
                    {
                        ZoneName = "Entryway",
                        ZoneNumber = 5,
                        PiId = "pr-rpi002",
                        IpAddress = "192.168.1.202",
                        Port = 5005,
                        Color = Color.Plum
                    }
                },
                { "Waiting Kennels", new ZoneConnection
                    {
                        ZoneName = "Waiting Kennels",
                        ZoneNumber = 6,
                        PiId = "pr-rpi002",
                        IpAddress = "192.168.1.202",
                        Port = 5006,
                        Color = Color.PeachPuff
                    }
                },
                { "Play Yard", new ZoneConnection
                    {
                        ZoneName = "Play Yard",
                        ZoneNumber = 7,
                        PiId = "pr-rpi002",
                        IpAddress = "192.168.1.202",
                        Port = 5007,
                        Color = Color.LightCyan
                    }
                },
                { "Pickup Kennel", new ZoneConnection
                    {
                        ZoneName = "Pickup Kennel",
                        ZoneNumber = 8,
                        PiId = "pr-rpi002",
                        IpAddress = "192.168.1.202",
                        Port = 5008,
                        Color = Color.LightSalmon
                    }
                }
            };
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Form properties
            this.Text = "Pea Ridge Dog Grooming - Multi-Zone Monitor";
            this.Size = new Size(900, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(800, 600);
            this.Icon = SystemIcons.Information;

            // Title Label
            Label titleLabel = new Label
            {
                Text = "🐕 Pea Ridge - 8 Zone RFID Tracking System",
                Font = new Font("Arial", 16, FontStyle.Bold),
                Location = new Point(20, 20),
                Size = new Size(850, 35),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.LightGray,
                ForeColor = Color.DarkBlue
            };
            this.Controls.Add(titleLabel);

            // Global Status
            lblGlobalStatus = new Label
            {
                Text = "Status: All Zones Disconnected",
                Location = new Point(20, 65),
                Size = new Size(400, 25),
                Font = new Font("Arial", 10, FontStyle.Bold),
                ForeColor = Color.Red
            };
            this.Controls.Add(lblGlobalStatus);

            // Connection Controls
            Panel connectionPanel = new Panel
            {
                Location = new Point(450, 65),
                Size = new Size(420, 30),
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(connectionPanel);

            btnConnectAll = new Button
            {
                Text = "🔌 Connect All Zones",
                Location = new Point(5, 2),
                Size = new Size(200, 25),
                BackColor = Color.LightGreen
            };
            btnConnectAll.Click += BtnConnectAll_Click;
            connectionPanel.Controls.Add(btnConnectAll);

            btnDisconnectAll = new Button
            {
                Text = "❌ Disconnect All",
                Location = new Point(210, 2),
                Size = new Size(200, 25),
                BackColor = Color.LightCoral,
                Enabled = false
            };
            btnDisconnectAll.Click += BtnDisconnectAll_Click;
            connectionPanel.Controls.Add(btnDisconnectAll);

            // Zone Status List
            GroupBox zoneStatusGroup = new GroupBox
            {
                Text = "Zone Connection Status",
                Location = new Point(20, 105),
                Size = new Size(850, 280),
                Font = new Font("Arial", 10, FontStyle.Bold)
            };
            this.Controls.Add(zoneStatusGroup);

            zoneStatusList = new ListView
            {
                Location = new Point(10, 25),
                Size = new Size(830, 245),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Font = new Font("Arial", 9)
            };

            zoneStatusList.Columns.Add("Zone #", 60);
            zoneStatusList.Columns.Add("Zone Name", 150);
            zoneStatusList.Columns.Add("Raspberry Pi", 100);
            zoneStatusList.Columns.Add("IP:Port", 140);
            zoneStatusList.Columns.Add("Status", 120);
            zoneStatusList.Columns.Add("Tags Read", 80);
            zoneStatusList.Columns.Add("Last Activity", 150);

            zoneStatusGroup.Controls.Add(zoneStatusList);

            // Populate zone status list
            foreach (var zone in zoneConnections.Values.OrderBy(z => z.ZoneNumber))
            {
                ListViewItem item = new ListViewItem(new string[]
                {
                    zone.ZoneNumber.ToString(),
                    zone.ZoneName,
                    zone.PiId,
                    $"{zone.IpAddress}:{zone.Port}",
                    "Disconnected",
                    "0",
                    "Never"
                });
                item.BackColor = zone.Color;
                item.Tag = zone;
                zoneStatusList.Items.Add(item);
            }

            // Controls Group
            GroupBox controlsGroup = new GroupBox
            {
                Text = "Controls",
                Location = new Point(20, 395),
                Size = new Size(850, 100),
                Font = new Font("Arial", 10, FontStyle.Bold)
            };
            this.Controls.Add(controlsGroup);

            btnShowQueue = new Button
            {
                Text = "📋 Show Customer Queue",
                Location = new Point(20, 25),
                Size = new Size(200, 35),
                BackColor = Color.LightGreen,
                Font = new Font("Arial", 10, FontStyle.Bold)
            };
            btnShowQueue.Click += BtnShowQueue_Click;
            controlsGroup.Controls.Add(btnShowQueue);

            btnViewDatabase = new Button
            {
                Text = "📊 View Customer Database",
                Location = new Point(230, 25),
                Size = new Size(200, 35),
                BackColor = Color.LightBlue
            };
            btnViewDatabase.Click += BtnViewDatabase_Click;
            controlsGroup.Controls.Add(btnViewDatabase);

            btnViewZoneHistory = new Button
            {
                Text = "📍 View Zone History",
                Location = new Point(440, 25),
                Size = new Size(180, 35),
                BackColor = Color.LightCyan
            };
            btnViewZoneHistory.Click += BtnViewZoneHistory_Click;
            controlsGroup.Controls.Add(btnViewZoneHistory);

            btnAddTag = new Button
            {
                Text = "➕ Add Tag",
                Location = new Point(630, 25),
                Size = new Size(100, 35),
                BackColor = Color.LightYellow
            };
            btnAddTag.Click += BtnAddTag_Click;
            controlsGroup.Controls.Add(btnAddTag);

            btnMinimize = new Button
            {
                Text = "⬇️ Minimize",
                Location = new Point(740, 25),
                Size = new Size(100, 35)
            };
            btnMinimize.Click += BtnMinimize_Click;
            controlsGroup.Controls.Add(btnMinimize);

            // Statistics Panel
            Panel statsPanel = new Panel
            {
                Location = new Point(20, 505),
                Size = new Size(850, 120),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.WhiteSmoke
            };
            this.Controls.Add(statsPanel);

            Label statsTitle = new Label
            {
                Text = "📈 System Statistics",
                Font = new Font("Arial", 12, FontStyle.Bold),
                Location = new Point(10, 10),
                Size = new Size(800, 25)
            };
            statsPanel.Controls.Add(statsTitle);

            // Stats will be updated dynamically
            CreateStatLabel(statsPanel, "Connected Zones:", "0/8", 10, 40, "lblConnectedZones");
            CreateStatLabel(statsPanel, "Total Tags Read Today:", "0", 220, 40, "lblTotalTags");
            CreateStatLabel(statsPanel, "Active Customers:", "0", 450, 40, "lblActiveCustomers");
            CreateStatLabel(statsPanel, "Last Read:", "None", 10, 70, "lblLastRead");

            // Form events
            this.FormClosing += MainForm_FormClosing;
            this.Resize += MainForm_Resize;

            this.ResumeLayout();
        }

        private void CreateStatLabel(Panel parent, string labelText, string value, int x, int y, string name)
        {
            Label lbl = new Label
            {
                Text = labelText,
                Location = new Point(x, y),
                Size = new Size(150, 20),
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
            parent.Controls.Add(lbl);

            Label val = new Label
            {
                Text = value,
                Name = name,
                Location = new Point(x + 155, y),
                Size = new Size(200, 20),
                Font = new Font("Arial", 9),
                ForeColor = Color.DarkBlue
            };
            parent.Controls.Add(val);
        }

        private void UpdateStats()
        {
            int connectedCount = zoneConnections.Values.Count(z => z.IsConnected);
            this.Invoke((Action)(() =>
            {
                Control statsPanel = this.Controls[6]; // Stats panel
                statsPanel.Controls["lblConnectedZones"].Text = $"{connectedCount}/8";
                
                // Update color based on connection status
                if (connectedCount == 8)
                    statsPanel.Controls["lblConnectedZones"].ForeColor = Color.Green;
                else if (connectedCount > 0)
                    statsPanel.Controls["lblConnectedZones"].ForeColor = Color.Orange;
                else
                    statsPanel.Controls["lblConnectedZones"].ForeColor = Color.Red;
            }));
        }

        private void BtnConnectAll_Click(object sender, EventArgs e)
        {
            btnConnectAll.Enabled = false;
            btnDisconnectAll.Enabled = true;

            foreach (var zone in zoneConnections.Values)
            {
                ConnectToZone(zone);
            }

            UpdateGlobalStatus();
        }

        private void BtnDisconnectAll_Click(object sender, EventArgs e)
        {
            foreach (var zone in zoneConnections.Values)
            {
                DisconnectFromZone(zone);
            }

            btnConnectAll.Enabled = true;
            btnDisconnectAll.Enabled = false;
            UpdateGlobalStatus();
        }

        private async void ConnectToZone(ZoneConnection zone)
        {
            try
            {
                UpdateZoneStatus(zone, "Connecting...", Color.Yellow);

                zone.TcpClient = new TcpClient();
                await zone.TcpClient.ConnectAsync(zone.IpAddress, zone.Port);
                zone.Stream = zone.TcpClient.GetStream();
                zone.IsConnected = true;
                zone.IsListening = true;

                UpdateZoneStatus(zone, "Connected ✓", Color.LightGreen);

                // Start listening thread for this zone
                zone.ListenThread = new Thread(() => ListenForTags(zone))
                {
                    IsBackground = true,
                    Name = $"Zone{zone.ZoneNumber}_Listener"
                };
                zone.ListenThread.Start();

                UpdateStats();
            }
            catch (Exception ex)
            {
                UpdateZoneStatus(zone, $"Failed: {ex.Message}", Color.Red);
                zone.IsConnected = false;
            }
        }

        private void DisconnectFromZone(ZoneConnection zone)
        {
            try
            {
                zone.IsConnected = false;
                zone.IsListening = false;

                zone.Stream?.Close();
                zone.TcpClient?.Close();

                UpdateZoneStatus(zone, "Disconnected", Color.LightGray);
                UpdateStats();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disconnecting zone {zone.ZoneName}: {ex.Message}");
            }
        }

        private void UpdateZoneStatus(ZoneConnection zone, string status, Color statusColor)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<ZoneConnection, string, Color>(UpdateZoneStatus), zone, status, statusColor);
                return;
            }

            foreach (ListViewItem item in zoneStatusList.Items)
            {
                if (((ZoneConnection)item.Tag).ZoneNumber == zone.ZoneNumber)
                {
                    item.SubItems[4].Text = status;
                    item.SubItems[4].BackColor = statusColor;
                    break;
                }
            }
        }

        private void UpdateZoneActivity(ZoneConnection zone, string tagId)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<ZoneConnection, string>(UpdateZoneActivity), zone, tagId);
                return;
            }

            foreach (ListViewItem item in zoneStatusList.Items)
            {
                if (((ZoneConnection)item.Tag).ZoneNumber == zone.ZoneNumber)
                {
                    zone.TagCount++;
                    item.SubItems[5].Text = zone.TagCount.ToString();
                    item.SubItems[6].Text = DateTime.Now.ToString("HH:mm:ss");
                    
                    // Flash the row
                    item.BackColor = Color.Yellow;
                    Timer flashTimer = new Timer { Interval = 500 };
                    flashTimer.Tick += (s, e) =>
                    {
                        item.BackColor = zone.Color;
                        flashTimer.Stop();
                        flashTimer.Dispose();
                    };
                    flashTimer.Start();
                    break;
                }
            }

            // Update last read stat
            Control statsPanel = this.Controls[6];
            statsPanel.Controls["lblLastRead"].Text = $"{zone.ZoneName} - {tagId.Substring(0, Math.Min(12, tagId.Length))}...";
        }

        private void UpdateGlobalStatus()
        {
            int connected = zoneConnections.Values.Count(z => z.IsConnected);
            int total = zoneConnections.Count;

            string statusText = $"Status: {connected}/{total} Zones Connected";
            Color statusColor;

            if (connected == total)
            {
                statusColor = Color.Green;
                statusText += " ✓ All Systems Online";
            }
            else if (connected > 0)
            {
                statusColor = Color.Orange;
                statusText += " ⚠ Partial Connection";
            }
            else
            {
                statusColor = Color.Red;
                statusText += " ✗ All Zones Offline";
            }

            this.Invoke((Action)(() =>
            {
                lblGlobalStatus.Text = statusText;
                lblGlobalStatus.ForeColor = statusColor;
            }));
        }

        private void ListenForTags(ZoneConnection zone)
        {
            byte[] buffer = new byte[1024];

            while (zone.IsListening && zone.TcpClient.Connected)
            {
                try
                {
                    int bytesRead = zone.Stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        // Convert to hex string
                        StringBuilder hexString = new StringBuilder();
                        for (int i = 0; i < bytesRead; i++)
                        {
                            hexString.Append(buffer[i].ToString("X2"));
                            if (i < bytesRead - 1) hexString.Append(" ");
                        }

                        string hexData = hexString.ToString();
                        string tagId = ProcessTagData(hexData);

                        if (!string.IsNullOrEmpty(tagId))
                        {
                            // Log to database with zone info
                            LogTagRead(tagId, zone);

                            // Update zone activity
                            UpdateZoneActivity(zone, tagId);

                            // Lookup customer info
                            TagInfo tagInfo = LookupTag(tagId);
                            tagInfo.CurrentZone = zone.ZoneName;
                            tagInfo.ZoneNumber = zone.ZoneNumber;
                            tagInfo.ZoneColor = zone.Color;

                            this.Invoke((Action)(() =>
                            {
                                PlayDingSound();

                                if (!queueForm.IsDisposed)
                                {
                                    queueForm.AddOrUpdateCustomer(tagInfo);
                                }

                                ShowTagPopup(tagInfo);
                            }));
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (zone.IsListening)
                    {
                        Console.WriteLine($"Zone {zone.ZoneName} listen error: {ex.Message}");
                        this.Invoke((Action)(() =>
                        {
                            UpdateZoneStatus(zone, "Error - Reconnecting...", Color.Orange);
                        }));
                        
                        // Attempt reconnect
                        Thread.Sleep(5000);
                        if (zone.IsListening)
                        {
                            DisconnectFromZone(zone);
                            Thread.Sleep(1000);
                            ConnectToZone(zone);
                        }
                    }
                    break;
                }
            }
        }

        private void LogTagRead(string tagId, ZoneConnection zone)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                        INSERT INTO tag_reads (tag_id, zone_number, zone_name, pi_id, pi_ip)
                        VALUES (@tagId, @zoneNumber, @zoneName, @piId, @piIp)";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@tagId", tagId);
                    cmd.Parameters.AddWithValue("@zoneNumber", zone.ZoneNumber);
                    cmd.Parameters.AddWithValue("@zoneName", zone.ZoneName);
                    cmd.Parameters.AddWithValue("@piId", zone.PiId);
                    cmd.Parameters.AddWithValue("@piIp", zone.IpAddress);
                    cmd.ExecuteNonQuery();
                }

                // Update total tags stat
                this.Invoke((Action)(() =>
                {
                    Control statsPanel = this.Controls[6];
                    int currentTotal = int.Parse(statsPanel.Controls["lblTotalTags"].Text);
                    statsPanel.Controls["lblTotalTags"].Text = (currentTotal + 1).ToString();
                }));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error logging tag read: {ex.Message}");
            }
        }

        private string ProcessTagData(string data)
        {
            try
            {
                if (IsHexData(data))
                {
                    return ProcessHexData(data);
                }

                if (data.StartsWith("{"))
                {
                    try
                    {
                        dynamic jsonData = JsonConvert.DeserializeObject(data);
                        return jsonData.rfid_tag ?? jsonData.tagId ?? jsonData.tag_id ?? jsonData.epc ?? data;
                    }
                    catch { }
                }

                return data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ProcessTagData error: {ex.Message}");
                return null;
            }
        }

        private bool IsHexData(string data)
        {
            return Regex.IsMatch(data, @"^[0-9A-Fa-f\s:]+$") && data.Contains(" ");
        }

        private string ProcessHexData(string hexData)
        {
            try
            {
                string cleanHex = hexData.Replace(" ", "").Replace(":", "").Replace("\r", "").Replace("\n", "");

                // Try to parse as JSON first (Pea Ridge sends JSON)
                StringBuilder asciiResult = new StringBuilder();
                for (int i = 0; i < cleanHex.Length; i += 2)
                {
                    if (i + 1 < cleanHex.Length)
                    {
                        string hexByte = cleanHex.Substring(i, 2);
                        try
                        {
                            int value = Convert.ToInt32(hexByte, 16);
                            if (value >= 32 && value <= 126)
                            {
                                asciiResult.Append((char)value);
                            }
                        }
                        catch { }
                    }
                }

                string asciiData = asciiResult.ToString();

                // Try to extract from JSON
                if (asciiData.Contains("rfid_tag"))
                {
                    try
                    {
                        dynamic json = JsonConvert.DeserializeObject(asciiData);
                        return json.rfid_tag;
                    }
                    catch { }
                }

                // Extract 24-character EPC
                Match match = Regex.Match(asciiData, @"([0-9A-F]{24})");
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ProcessHexData error: {ex.Message}");
                return null;
            }
        }

        private void PlayDingSound()
        {
            try
            {
                SystemSounds.Beep.Play();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not play sound: {ex.Message}");
            }
        }

        private TagInfo LookupTag(string tagId)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    string updateQuery = "UPDATE rfid_tags SET last_seen = NOW() WHERE tag_id = @tagId";
                    MySqlCommand updateCmd = new MySqlCommand(updateQuery, conn);
                    updateCmd.Parameters.AddWithValue("@tagId", tagId);
                    updateCmd.ExecuteNonQuery();

                    string selectQuery = @"
                        SELECT tag_id, customer_name, dogs_name, dogs_breed, vehicle_make_color, notes, last_seen
                        FROM rfid_tags WHERE tag_id = @tagId";

                    MySqlCommand selectCmd = new MySqlCommand(selectQuery, conn);
                    selectCmd.Parameters.AddWithValue("@tagId", tagId);

                    using (MySqlDataReader reader = selectCmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new TagInfo
                            {
                                Found = true,
                                TagId = reader["tag_id"].ToString(),
                                CustomerName = reader["customer_name"].ToString(),
                                Dogsname = reader["dogs_name"].ToString(),
                                Dogsbreed = reader["dogs_breed"].ToString(),
                                Vehicle = reader["vehicle_make_color"].ToString(),
                                Notes = reader["notes"].ToString(),
                                LastSeen = reader["last_seen"].ToString()
                            };
                        }
                    }
                }

                return new TagInfo { Found = false, TagId = tagId };
            }
            catch (Exception ex)
            {
                return new TagInfo { Found = false, TagId = tagId, Error = ex.Message };
            }
        }

        private void ShowTagPopup(TagInfo tagInfo)
        {
            TagPopupForm popup = new TagPopupForm(tagInfo);
            popup.ShowDialog(this);
        }

        private void BtnShowQueue_Click(object sender, EventArgs e)
        {
            if (queueForm.IsDisposed)
            {
                queueForm = new CustomerQueueForm(connectionString);
            }
            queueForm.Show();
            queueForm.BringToFront();
        }

        private void BtnViewDatabase_Click(object sender, EventArgs e)
        {
            DatabaseViewForm dbForm = new DatabaseViewForm(connectionString);
            dbForm.ShowDialog(this);
        }

        private void BtnViewZoneHistory_Click(object sender, EventArgs e)
        {
            ZoneHistoryForm historyForm = new ZoneHistoryForm(connectionString);
            historyForm.ShowDialog(this);
        }

        private void BtnAddTag_Click(object sender, EventArgs e)
        {
            AddTagForm addForm = new AddTagForm(connectionString);
            addForm.ShowDialog(this);
        }

        private void BtnMinimize_Click(object sender, EventArgs e)
        {
            MinimizeToTray();
        }

        private void SetupNotifyIcon()
        {
            notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Information,
                Text = "Pea Ridge RFID Monitor",
                Visible = false
            };

            ContextMenuStrip contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Restore", null, (s, e) => RestoreFromTray());
            contextMenu.Items.Add("Exit", null, (s, e) => Application.Exit());
            notifyIcon.ContextMenuStrip = contextMenu;
            notifyIcon.DoubleClick += (s, e) => RestoreFromTray();
        }

        private void MinimizeToTray()
        {
            this.Hide();
            notifyIcon.Visible = true;
            notifyIcon.ShowBalloonTip(3000, "Pea Ridge Monitor", "Application minimized to system tray", ToolTipIcon.Info);
        }

        private void RestoreFromTray()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            notifyIcon.Visible = false;
            this.Activate();
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                MinimizeToTray();
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                MinimizeToTray();
            }
        }

        private void SetupDatabase()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // Create rfid_tags table
                    string createTagsTable = @"
                        CREATE TABLE IF NOT EXISTS rfid_tags (
                            id INT AUTO_INCREMENT PRIMARY KEY,
                            tag_id VARCHAR(255) UNIQUE NOT NULL,
                            customer_name VARCHAR(255),
                            dogs_name VARCHAR(255),
                            dogs_breed VARCHAR(255),
                            vehicle_make_color TEXT,
                            notes TEXT,
                            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                            last_seen TIMESTAMP NULL
                        )";
                    MySqlCommand cmd = new MySqlCommand(createTagsTable, conn);
                    cmd.ExecuteNonQuery();

                    // Create tag_reads table for zone tracking
                    string createReadsTable = @"
                        CREATE TABLE IF NOT EXISTS tag_reads (
                            id INT AUTO_INCREMENT PRIMARY KEY,
                            tag_id VARCHAR(255) NOT NULL,
                            zone_number INT NOT NULL,
                            zone_name VARCHAR(100) NOT NULL,
                            pi_id VARCHAR(50) NOT NULL,
                            pi_ip VARCHAR(15) NOT NULL,
                            timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                            INDEX idx_tag_id (tag_id),
                            INDEX idx_timestamp (timestamp),
                            INDEX idx_zone (zone_number)
                        )";
                    cmd = new MySqlCommand(createReadsTable, conn);
                    cmd.ExecuteNonQuery();

                    // Sample data (remove before production)
                    string[] sampleData = {
                        "('E20034120000001D00000000', 'Scott Melton', 'Buddy', 'Golden Retriever', 'Toyota Black', 'VIP Customer')",
                        "('E20034120000001D00000001', 'Whitney Kinnaman', 'Luna', 'Border Collie', 'Ford PU White', 'Premium Member')",
                        "('E20034120000001D00000002', 'Mike Davis', 'Max', 'German Shepherd', 'Dodge Ram Blue', 'Regular Customer')"
                    };

                    foreach (string data in sampleData)
                    {
                        string insertSample = $@"
                            INSERT IGNORE INTO rfid_tags 
                            (tag_id, customer_name, dogs_name, dogs_breed, vehicle_make_color, notes)
                            VALUES {data}";
                        cmd = new MySqlCommand(insertSample, conn);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database setup error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var zone in zoneConnections.Values)
                {
                    DisconnectFromZone(zone);
                }
                notifyIcon?.Dispose();
                queueForm?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    // Zone Connection Class
    public class ZoneConnection
    {
        public string ZoneName { get; set; }
        public int ZoneNumber { get; set; }
        public string PiId { get; set; }
        public string IpAddress { get; set; }
        public int Port { get; set; }
        public Color Color { get; set; }
        
        public TcpClient TcpClient { get; set; }
        public NetworkStream Stream { get; set; }
        public Thread ListenThread { get; set; }
        public bool IsConnected { get; set; }
        public bool IsListening { get; set; }
        public int TagCount { get; set; }
    }

    // TagInfo Class (updated with zone info)
    public class TagInfo
    {
        public bool Found { get; set; }
        public string TagId { get; set; }
        public string CustomerName { get; set; }
        public string Dogsname { get; set; }
        public string Dogsbreed { get; set; }
        public string Vehicle { get; set; }
        public string Notes { get; set; }
        public string LastSeen { get; set; }
        public string Error { get; set; }
        public string CurrentZone { get; set; }
        public int ZoneNumber { get; set; }
        public Color ZoneColor { get; set; }
    }

    // Continued in next file...
}
