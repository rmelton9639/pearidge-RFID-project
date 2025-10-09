using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace RFIDLookupApp
{
    // Tag Popup Form with Zone Info
    public partial class TagPopupForm : Form
    {
        public TagPopupForm(TagInfo tagInfo)
        {
            InitializeComponent();
            DisplayTagInfo(tagInfo);

            // Auto-close after 5 seconds
            System.Windows.Forms.Timer closeTimer = new System.Windows.Forms.Timer
            {
                Interval = 5000
            };
            closeTimer.Tick += (s, e) => { closeTimer.Stop(); this.Close(); };
            closeTimer.Start();
        }

        private void InitializeComponent()
        {
            this.Text = "🐕 RFID Tag Detected";
            this.Size = new Size(500, 450);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.TopMost = true;
        }

        private void DisplayTagInfo(TagInfo tagInfo)
        {
            this.Controls.Clear();
            Panel mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                BackColor = Color.White
            };
            this.Controls.Add(mainPanel);

            int yPos = 20;

            if (tagInfo.Found && string.IsNullOrEmpty(tagInfo.Error))
            {
                // Zone indicator at top
                Panel zonePanel = new Panel
                {
                    Location = new Point(20, yPos),
                    Size = new Size(420, 40),
                    BackColor = tagInfo.ZoneColor,
                    BorderStyle = BorderStyle.FixedSingle
                };
                mainPanel.Controls.Add(zonePanel);

                Label zoneLabel = new Label
                {
                    Text = $"📍 ZONE {tagInfo.ZoneNumber}: {tagInfo.CurrentZone}",
                    Font = new Font("Arial", 14, FontStyle.Bold),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = Color.DarkBlue
                };
                zonePanel.Controls.Add(zoneLabel);
                yPos += 50;

                // Tag found
                Label titleLabel = new Label
                {
                    Text = "✓ Customer Detected!",
                    Font = new Font("Arial", 16, FontStyle.Bold),
                    ForeColor = Color.Green,
                    Location = new Point(20, yPos),
                    Size = new Size(400, 30)
                };
                mainPanel.Controls.Add(titleLabel);
                yPos += 50;

                // Customer info
                string[] labels = { "Tag ID:", "Customer:", "Dog's Name:", "Dog's Breed:", 
                                   "Vehicle:", "Notes:", "Last Seen:" };
                string[] values = { tagInfo.TagId, tagInfo.CustomerName, tagInfo.Dogsname,
                                  tagInfo.Dogsbreed, tagInfo.Vehicle, tagInfo.Notes, tagInfo.LastSeen };

                for (int i = 0; i < labels.Length; i++)
                {
                    Label lblField = new Label
                    {
                        Text = labels[i],
                        Font = new Font("Arial", 10, FontStyle.Bold),
                        Location = new Point(20, yPos),
                        Size = new Size(120, 25)
                    };
                    mainPanel.Controls.Add(lblField);

                    Label lblValue = new Label
                    {
                        Text = values[i] ?? "N/A",
                        Location = new Point(145, yPos),
                        Size = new Size(300, 25),
                        Font = new Font("Arial", 10)
                    };
                    mainPanel.Controls.Add(lblValue);

                    yPos += 30;
                }
            }
            else if (!string.IsNullOrEmpty(tagInfo.Error))
            {
                // Error
                Label titleLabel = new Label
                {
                    Text = "⚠ Database Error",
                    Font = new Font("Arial", 16, FontStyle.Bold),
                    ForeColor = Color.Red,
                    Location = new Point(20, yPos),
                    Size = new Size(400, 30)
                };
                mainPanel.Controls.Add(titleLabel);
                yPos += 50;

                Label errorLabel = new Label
                {
                    Text = tagInfo.Error,
                    Location = new Point(20, yPos),
                    Size = new Size(420, 80),
                    Font = new Font("Arial", 10)
                };
                mainPanel.Controls.Add(errorLabel);
                yPos += 100;
            }
            else
            {
                // Tag not found - show zone anyway
                Panel zonePanel = new Panel
                {
                    Location = new Point(20, yPos),
                    Size = new Size(420, 40),
                    BackColor = tagInfo.ZoneColor,
                    BorderStyle = BorderStyle.FixedSingle
                };
                mainPanel.Controls.Add(zonePanel);

                Label zoneLabel = new Label
                {
                    Text = $"📍 ZONE {tagInfo.ZoneNumber}: {tagInfo.CurrentZone}",
                    Font = new Font("Arial", 14, FontStyle.Bold),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = Color.DarkBlue
                };
                zonePanel.Controls.Add(zoneLabel);
                yPos += 50;

                Label titleLabel = new Label
                {
                    Text = "✗ Tag Not Found",
                    Font = new Font("Arial", 16, FontStyle.Bold),
                    ForeColor = Color.Red,
                    Location = new Point(20, yPos),
                    Size = new Size(400, 30)
                };
                mainPanel.Controls.Add(titleLabel);
                yPos += 50;

                Label tagLabel = new Label
                {
                    Text = $"Tag ID: {tagInfo.TagId}",
                    Font = new Font("Arial", 11, FontStyle.Bold),
                    Location = new Point(20, yPos),
                    Size = new Size(400, 25)
                };
                mainPanel.Controls.Add(tagLabel);
                yPos += 40;

                Label msgLabel = new Label
                {
                    Text = "This tag is not registered in the database.\nPlease add customer information.",
                    Location = new Point(20, yPos),
                    Size = new Size(400, 40),
                    Font = new Font("Arial", 10)
                };
                mainPanel.Controls.Add(msgLabel);
                yPos += 60;
            }

            // Close button
            Button closeButton = new Button
            {
                Text = "Close",
                Location = new Point(190, yPos),
                Size = new Size(100, 35),
                Font = new Font("Arial", 10, FontStyle.Bold)
            };
            closeButton.Click += (s, e) => this.Close();
            mainPanel.Controls.Add(closeButton);
        }
    }

    // Zone History Form
    public partial class ZoneHistoryForm : Form
    {
        private string connectionString;
        private DataGridView historyGrid;
        private ComboBox filterCombo;
        private DateTimePicker dateFrom;
        private DateTimePicker dateTo;

        public ZoneHistoryForm(string connString)
        {
            connectionString = connString;
            InitializeComponent();
            LoadZoneHistory();
        }

        private void InitializeComponent()
        {
            this.Text = "📍 Zone History - All Tag Reads";
            this.Size = new Size(1100, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Filter panel
            Panel filterPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.LightGray,
                Padding = new Padding(10)
            };
            this.Controls.Add(filterPanel);

            Label lblFilter = new Label
            {
                Text = "Filter by Zone:",
                Location = new Point(10, 15),
                Size = new Size(100, 20),
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
            filterPanel.Controls.Add(lblFilter);

            filterCombo = new ComboBox
            {
                Location = new Point(110, 12),
                Size = new Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            filterCombo.Items.AddRange(new object[] {
                "All Zones",
                "Zone 1 - Bathing",
                "Zone 2 - Drying",
                "Zone 3 - Grooming Room 1",
                "Zone 4 - Grooming Room 2",
                "Zone 5 - Entryway",
                "Zone 6 - Waiting Kennels",
                "Zone 7 - Play Yard",
                "Zone 8 - Pickup Kennel"
            });
            filterCombo.SelectedIndex = 0;
            filterPanel.Controls.Add(filterCombo);

            Label lblDateFrom = new Label
            {
                Text = "From:",
                Location = new Point(330, 15),
                Size = new Size(50, 20),
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
            filterPanel.Controls.Add(lblDateFrom);

            dateFrom = new DateTimePicker
            {
                Location = new Point(380, 12),
                Size = new Size(150, 25),
                Value = DateTime.Now.AddDays(-7)
            };
            filterPanel.Controls.Add(dateFrom);

            Label lblDateTo = new Label
            {
                Text = "To:",
                Location = new Point(550, 15),
                Size = new Size(30, 20),
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
            filterPanel.Controls.Add(lblDateTo);

            dateTo = new DateTimePicker
            {
                Location = new Point(580, 12),
                Size = new Size(150, 25),
                Value = DateTime.Now
            };
            filterPanel.Controls.Add(dateTo);

            Button btnFilter = new Button
            {
                Text = "Apply Filter",
                Location = new Point(750, 10),
                Size = new Size(100, 30),
                BackColor = Color.LightBlue
            };
            btnFilter.Click += (s, e) => LoadZoneHistory();
            filterPanel.Controls.Add(btnFilter);

            Button btnExport = new Button
            {
                Text = "Export to CSV",
                Location = new Point(860, 10),
                Size = new Size(120, 30),
                BackColor = Color.LightGreen
            };
            btnExport.Click += BtnExport_Click;
            filterPanel.Controls.Add(btnExport);

            // Stats label
            Label statsLabel = new Label
            {
                Name = "statsLabel",
                Text = "Loading...",
                Location = new Point(10, 50),
                Size = new Size(900, 20),
                Font = new Font("Arial", 9)
            };
            filterPanel.Controls.Add(statsLabel);

            // DataGrid
            historyGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            this.Controls.Add(historyGrid);

            // Close button
            Button closeButton = new Button
            {
                Text = "Close",
                Dock = DockStyle.Bottom,
                Height = 40,
                Font = new Font("Arial", 10, FontStyle.Bold)
            };
            closeButton.Click += (s, e) => this.Close();
            this.Controls.Add(closeButton);
        }

        private void LoadZoneHistory()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    string query = @"
                        SELECT 
                            tr.id,
                            tr.tag_id,
                            tr.zone_number,
                            tr.zone_name,
                            tr.pi_id,
                            tr.timestamp,
                            rt.customer_name,
                            rt.dogs_name
                        FROM tag_reads tr
                        LEFT JOIN rfid_tags rt ON tr.tag_id = rt.tag_id
                        WHERE tr.timestamp >= @dateFrom AND tr.timestamp <= @dateTo";

                    // Add zone filter if selected
                    if (filterCombo.SelectedIndex > 0)
                    {
                        int zoneNum = filterCombo.SelectedIndex;
                        query += " AND tr.zone_number = @zoneNum";
                    }

                    query += " ORDER BY tr.timestamp DESC LIMIT 1000";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@dateFrom", dateFrom.Value.Date);
                    cmd.Parameters.AddWithValue("@dateTo", dateTo.Value.Date.AddDays(1).AddSeconds(-1));
                    
                    if (filterCombo.SelectedIndex > 0)
                    {
                        cmd.Parameters.AddWithValue("@zoneNum", filterCombo.SelectedIndex);
                    }

                    DataTable dt = new DataTable();
                    MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                    adapter.Fill(dt);

                    historyGrid.DataSource = dt;

                    // Update stats
                    Panel filterPanel = this.Controls[0] as Panel;
                    Label statsLabel = filterPanel.Controls["statsLabel"] as Label;
                    statsLabel.Text = $"Showing {dt.Rows.Count} reads from {dateFrom.Value.ToShortDateString()} to {dateTo.Value.ToShortDateString()}";

                    // Color code rows by zone
                    Color[] zoneColors = { Color.White, Color.LightBlue, Color.LightCoral, Color.LightGreen, 
                                          Color.LightGoldenrodYellow, Color.Plum, Color.PeachPuff, 
                                          Color.LightCyan, Color.LightSalmon };
                    
                    foreach (DataGridViewRow row in historyGrid.Rows)
                    {
                        if (row.Cells["zone_number"].Value != null)
                        {
                            int zoneNum = Convert.ToInt32(row.Cells["zone_number"].Value);
                            if (zoneNum >= 1 && zoneNum <= 8)
                            {
                                row.DefaultCellStyle.BackColor = zoneColors[zoneNum];
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading zone history: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            try
            {
                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv",
                    FileName = $"ZoneHistory_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    DataTable dt = historyGrid.DataSource as DataTable;
                    System.Text.StringBuilder csv = new System.Text.StringBuilder();

                    // Headers
                    foreach (DataColumn col in dt.Columns)
                    {
                        csv.Append(col.ColumnName + ",");
                    }
                    csv.AppendLine();

                    // Data
                    foreach (DataRow row in dt.Rows)
                    {
                        foreach (object item in row.ItemArray)
                        {
                            csv.Append(item.ToString().Replace(",", ";") + ",");
                        }
                        csv.AppendLine();
                    }

                    System.IO.File.WriteAllText(saveDialog.FileName, csv.ToString());
                    MessageBox.Show($"Exported {dt.Rows.Count} records to {saveDialog.FileName}", 
                        "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export error: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    // Database View Form
    public partial class DatabaseViewForm : Form
    {
        private string connectionString;

        public DatabaseViewForm(string connString)
        {
            connectionString = connString;
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = "📊 RFID Customer Database";
            this.Size = new Size(1000, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            DataGridView dataGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false
            };
            this.Controls.Add(dataGrid);

            Panel buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 45,
                BackColor = Color.LightGray
            };
            this.Controls.Add(buttonPanel);

            Button refreshButton = new Button
            {
                Text = "🔄 Refresh",
                Location = new Point(10, 8),
                Size = new Size(100, 30),
                BackColor = Color.LightGreen
            };
            refreshButton.Click += (s, e) => LoadData();
            buttonPanel.Controls.Add(refreshButton);

            Button closeButton = new Button
            {
                Text = "Close",
                Location = new Point(120, 8),
                Size = new Size(100, 30)
            };
            closeButton.Click += (s, e) => this.Close();
            buttonPanel.Controls.Add(closeButton);
        }

        private void LoadData()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT 
                            tag_id, 
                            customer_name, 
                            dogs_name, 
                            dogs_breed, 
                            vehicle_make_color, 
                            notes,
                            created_at,
                            last_seen
                        FROM rfid_tags
                        ORDER BY last_seen DESC";
                    
                    MySqlDataAdapter adapter = new MySqlDataAdapter(query, conn);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    DataGridView grid = this.Controls[0] as DataGridView;
                    grid.DataSource = dt;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    // Add Tag Form
    public partial class AddTagForm : Form
    {
        private string connectionString;
        private TextBox[] textBoxes;

        public AddTagForm(string connString)
        {
            connectionString = connString;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "➕ Add New RFID Tag";
            this.Size = new Size(450, 400);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            Label titleLabel = new Label
            {
                Text = "Register New Customer",
                Font = new Font("Arial", 14, FontStyle.Bold),
                Location = new Point(20, 20),
                Size = new Size(400, 30),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(titleLabel);

            string[] labels = { "Tag ID:", "Customer Name:", "Dog's Name:", "Dog's Breed:", 
                               "Vehicle Make/Color:", "Notes:" };
            textBoxes = new TextBox[labels.Length];

            int yPos = 70;
            for (int i = 0; i < labels.Length; i++)
            {
                Label label = new Label
                {
                    Text = labels[i],
                    Location = new Point(30, yPos),
                    Size = new Size(150, 20),
                    Font = new Font("Arial", 10, FontStyle.Bold)
                };
                this.Controls.Add(label);

                textBoxes[i] = new TextBox
                {
                    Location = new Point(190, yPos - 2),
                    Size = new Size(220, 20),
                    Font = new Font("Arial", 10)
                };
                this.Controls.Add(textBoxes[i]);

                yPos += 40;
            }

            Button saveButton = new Button
            {
                Text = "💾 Save",
                Location = new Point(190, yPos + 15),
                Size = new Size(100, 35),
                BackColor = Color.LightGreen,
                Font = new Font("Arial", 10, FontStyle.Bold)
            };
            saveButton.Click += SaveButton_Click;
            this.Controls.Add(saveButton);

            Button cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(300, yPos + 15),
                Size = new Size(100, 35),
                Font = new Font("Arial", 10)
            };
            cancelButton.Click += (s, e) => this.Close();
            this.Controls.Add(cancelButton);
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(textBoxes[0].Text))
                {
                    MessageBox.Show("Tag ID is required", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                        INSERT INTO rfid_tags (tag_id, customer_name, dogs_name, dogs_breed, vehicle_make_color, notes)
                        VALUES (@tagId, @customerName, @dogsname, @dogsbreed, @vehicle, @notes)";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@tagId", textBoxes[0].Text.Trim());
                    cmd.Parameters.AddWithValue("@customerName", textBoxes[1].Text.Trim());
                    cmd.Parameters.AddWithValue("@dogsname", textBoxes[2].Text.Trim());
                    cmd.Parameters.AddWithValue("@dogsbreed", textBoxes[3].Text.Trim());
                    cmd.Parameters.AddWithValue("@vehicle", textBoxes[4].Text.Trim());
                    cmd.Parameters.AddWithValue("@notes", textBoxes[5].Text.Trim());

                    cmd.ExecuteNonQuery();

                    MessageBox.Show("Customer added successfully!", "Success", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding tag: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    // Program entry point
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
