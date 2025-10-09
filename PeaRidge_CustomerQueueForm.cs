using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Windows.Forms;

namespace RFIDLookupApp
{
    // Customer Queue Form with Zone Tracking
    public partial class CustomerQueueForm : Form
    {
        private ListView customerListView;
        private Label statusLabel;
        private List<QueueItem> queueItems;
        private string connectionString;

        public CustomerQueueForm(string connString)
        {
            connectionString = connString;
            queueItems = new List<QueueItem>();
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "🐕 Pea Ridge Customer Queue - Lobby Monitor";
            this.Size = new Size(1000, 600);
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(100, 100);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(900, 500);

            // Status label
            statusLabel = new Label
            {
                Text = "Customer Queue - Click on completed customers to remove them",
                Font = new Font("Arial", 12, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 50,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.DarkBlue,
                ForeColor = Color.White,
                Padding = new Padding(5)
            };
            this.Controls.Add(statusLabel);

            // Legend Panel
            Panel legendPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.WhiteSmoke,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(legendPanel);

            Label legendTitle = new Label
            {
                Text = "Zone Color Legend:",
                Font = new Font("Arial", 10, FontStyle.Bold),
                Location = new Point(10, 5),
                Size = new Size(150, 20)
            };
            legendPanel.Controls.Add(legendTitle);

            // Zone legend
            string[] zones = { "1-Bathing", "2-Drying", "3-Groom 1", "4-Groom 2", 
                              "5-Entryway", "6-Waiting", "7-Play Yard", "8-Pickup" };
            Color[] colors = { Color.LightBlue, Color.LightCoral, Color.LightGreen, Color.LightGoldenrodYellow,
                              Color.Plum, Color.PeachPuff, Color.LightCyan, Color.LightSalmon };

            for (int i = 0; i < zones.Length; i++)
            {
                int row = i / 4;
                int col = i % 4;
                
                Panel colorBox = new Panel
                {
                    Location = new Point(10 + (col * 230), 30 + (row * 25)),
                    Size = new Size(20, 20),
                    BackColor = colors[i],
                    BorderStyle = BorderStyle.FixedSingle
                };
                legendPanel.Controls.Add(colorBox);

                Label zoneName = new Label
                {
                    Text = zones[i],
                    Location = new Point(35 + (col * 230), 30 + (row * 25)),
                    Size = new Size(180, 20),
                    Font = new Font("Arial", 9)
                };
                legendPanel.Controls.Add(zoneName);
            }

            // Customer list view
            customerListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Font = new Font("Arial", 10)
            };

            // Add columns with zone info
            customerListView.Columns.Add("Time", 80);
            customerListView.Columns.Add("Current Zone", 140);
            customerListView.Columns.Add("Customer Name", 150);
            customerListView.Columns.Add("Dog Name", 120);
            customerListView.Columns.Add("Dog Breed", 120);
            customerListView.Columns.Add("Vehicle", 150);
            customerListView.Columns.Add("Notes", 120);
            customerListView.Columns.Add("Zone History", 100);

            customerListView.DoubleClick += CustomerListView_DoubleClick;
            customerListView.KeyDown += CustomerListView_KeyDown;
            customerListView.MouseClick += CustomerListView_MouseClick;

            this.Controls.Add(customerListView);

            // Bottom button panel
            Panel buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 45,
                BackColor = Color.LightGray
            };
            this.Controls.Add(buttonPanel);

            Button clearAllButton = new Button
            {
                Text = "🗑️ Clear All Customers",
                Location = new Point(10, 8),
                Size = new Size(180, 30),
                BackColor = Color.Orange,
                Font = new Font("Arial", 10, FontStyle.Bold)
            };
            clearAllButton.Click += ClearAllButton_Click;
            buttonPanel.Controls.Add(clearAllButton);

            Button viewHistoryButton = new Button
            {
                Text = "📍 View Zone History",
                Location = new Point(200, 8),
                Size = new Size(180, 30),
                BackColor = Color.LightCyan,
                Font = new Font("Arial", 10, FontStyle.Bold)
            };
            viewHistoryButton.Click += ViewHistoryButton_Click;
            buttonPanel.Controls.Add(viewHistoryButton);

            Button refreshButton = new Button
            {
                Text = "🔄 Refresh",
                Location = new Point(390, 8),
                Size = new Size(100, 30),
                BackColor = Color.LightGreen
            };
            refreshButton.Click += (s, e) => UpdateStatus();
            buttonPanel.Controls.Add(refreshButton);

            UpdateStatus();
        }

        private void CustomerListView_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && customerListView.SelectedItems.Count > 0)
            {
                // Show context menu for zone history
                ContextMenuStrip menu = new ContextMenuStrip();
                menu.Items.Add("View Zone History", null, (s, ev) => ShowZoneHistoryForSelected());
                menu.Items.Add("Remove from Queue", null, (s, ev) => RemoveSelectedCustomers());
                menu.Show(customerListView, e.Location);
            }
        }

        private void ShowZoneHistoryForSelected()
        {
            if (customerListView.SelectedItems.Count > 0)
            {
                QueueItem item = customerListView.SelectedItems[0].Tag as QueueItem;
                if (item != null)
                {
                    ShowZoneHistory(item.TagInfo.TagId);
                }
            }
        }

        private void ShowZoneHistory(string tagId)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT zone_number, zone_name, timestamp 
                        FROM tag_reads 
                        WHERE tag_id = @tagId 
                        ORDER BY timestamp DESC 
                        LIMIT 50";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@tagId", tagId);

                    DataTable dt = new DataTable();
                    MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                    adapter.Fill(dt);

                    Form historyForm = new Form
                    {
                        Text = $"Zone History for Tag: {tagId}",
                        Size = new Size(600, 400),
                        StartPosition = FormStartPosition.CenterParent
                    };

                    DataGridView grid = new DataGridView
                    {
                        Dock = DockStyle.Fill,
                        DataSource = dt,
                        ReadOnly = true,
                        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
                    };
                    historyForm.Controls.Add(grid);

                    Button closeBtn = new Button
                    {
                        Text = "Close",
                        Dock = DockStyle.Bottom,
                        Height = 35
                    };
                    closeBtn.Click += (s, e) => historyForm.Close();
                    historyForm.Controls.Add(closeBtn);

                    historyForm.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading zone history: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CustomerListView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete && customerListView.SelectedItems.Count > 0)
            {
                RemoveSelectedCustomers();
            }
        }

        private void CustomerListView_DoubleClick(object sender, EventArgs e)
        {
            if (customerListView.SelectedItems.Count > 0)
            {
                RemoveSelectedCustomers();
            }
        }

        private void RemoveSelectedCustomers()
        {
            try
            {
                List<ListViewItem> itemsToRemove = new List<ListViewItem>();

                foreach (ListViewItem item in customerListView.SelectedItems)
                {
                    itemsToRemove.Add(item);
                }

                foreach (ListViewItem item in itemsToRemove)
                {
                    QueueItem queueItem = item.Tag as QueueItem;
                    if (queueItem != null)
                    {
                        queueItems.Remove(queueItem);
                    }
                    customerListView.Items.Remove(item);
                }

                UpdateStatus();
                SystemSounds.Hand.Play();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error removing customer: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearAllButton_Click(object sender, EventArgs e)
        {
            if (customerListView.Items.Count > 0)
            {
                DialogResult result = MessageBox.Show(
                    "Are you sure you want to clear all customers from the queue?",
                    "Clear All Customers",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    customerListView.Items.Clear();
                    queueItems.Clear();
                    UpdateStatus();
                    SystemSounds.Hand.Play();
                }
            }
        }

        private void ViewHistoryButton_Click(object sender, EventArgs e)
        {
            ZoneHistoryForm historyForm = new ZoneHistoryForm(connectionString);
            historyForm.ShowDialog(this);
        }

        public void AddOrUpdateCustomer(TagInfo tagInfo)
        {
            try
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action<TagInfo>(AddOrUpdateCustomer), tagInfo);
                    return;
                }

                string customerName = tagInfo.Found ? tagInfo.CustomerName : "Unknown Customer";
                
                // Find existing customer in queue
                QueueItem existingItem = queueItems.FirstOrDefault(q => q.TagInfo.TagId == tagInfo.TagId);

                if (existingItem != null)
                {
                    // Update existing customer's zone
                    existingItem.TagInfo.CurrentZone = tagInfo.CurrentZone;
                    existingItem.TagInfo.ZoneNumber = tagInfo.ZoneNumber;
                    existingItem.TagInfo.ZoneColor = tagInfo.ZoneColor;
                    existingItem.LastUpdate = DateTime.Now;
                    existingItem.ZoneHistory.Add(new ZoneVisit 
                    { 
                        ZoneName = tagInfo.CurrentZone, 
                        ZoneNumber = tagInfo.ZoneNumber,
                        Timestamp = DateTime.Now 
                    });

                    // Update ListView
                    foreach (ListViewItem listItem in customerListView.Items)
                    {
                        if (((QueueItem)listItem.Tag).TagInfo.TagId == tagInfo.TagId)
                        {
                            listItem.SubItems[0].Text = DateTime.Now.ToString("HH:mm:ss");
                            listItem.SubItems[1].Text = $"Zone {tagInfo.ZoneNumber}: {tagInfo.CurrentZone}";
                            listItem.SubItems[1].BackColor = tagInfo.ZoneColor;
                            listItem.SubItems[7].Text = existingItem.ZoneHistory.Count.ToString();

                            // Flash animation
                            listItem.BackColor = Color.Yellow;
                            Timer flashTimer = new Timer { Interval = 800 };
                            flashTimer.Tick += (s, e) =>
                            {
                                listItem.BackColor = Color.White;
                                flashTimer.Stop();
                                flashTimer.Dispose();
                            };
                            flashTimer.Start();
                            break;
                        }
                    }
                }
                else
                {
                    // Add new customer
                    QueueItem newItem = new QueueItem
                    {
                        ArrivalTime = DateTime.Now,
                        LastUpdate = DateTime.Now,
                        TagInfo = tagInfo,
                        ZoneHistory = new List<ZoneVisit>
                        {
                            new ZoneVisit 
                            { 
                                ZoneName = tagInfo.CurrentZone,
                                ZoneNumber = tagInfo.ZoneNumber,
                                Timestamp = DateTime.Now 
                            }
                        }
                    };

                    queueItems.Add(newItem);

                    ListViewItem listItem = new ListViewItem(new string[]
                    {
                        newItem.ArrivalTime.ToString("HH:mm:ss"),
                        $"Zone {tagInfo.ZoneNumber}: {tagInfo.CurrentZone}",
                        tagInfo.Found ? tagInfo.CustomerName : "Unknown Customer",
                        tagInfo.Found ? tagInfo.Dogsname : "N/A",
                        tagInfo.Found ? tagInfo.Dogsbreed : "N/A",
                        tagInfo.Found ? tagInfo.Vehicle : "N/A",
                        tagInfo.Found ? tagInfo.Notes : $"Tag: {tagInfo.TagId}",
                        "1"
                    })
                    {
                        Tag = newItem,
                        BackColor = tagInfo.Found ? Color.White : Color.LightCoral
                    };

                    // Color the zone column
                    listItem.SubItems[1].BackColor = tagInfo.ZoneColor;
                    listItem.SubItems[1].ForeColor = Color.Black;

                    customerListView.Items.Add(listItem);
                    listItem.EnsureVisible();
                }

                UpdateStatus();

                // Bring window to front when new customer arrives
                if (!this.Visible)
                {
                    this.Show();
                }
                this.BringToFront();
                this.FlashWindow();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding/updating customer in queue: {ex.Message}");
            }
        }

        private void UpdateStatus()
        {
            int customerCount = customerListView.Items.Count;
            
            // Count customers by zone
            Dictionary<int, int> zoneCounts = new Dictionary<int, int>();
            foreach (QueueItem item in queueItems)
            {
                int zoneNum = item.TagInfo.ZoneNumber;
                if (!zoneCounts.ContainsKey(zoneNum))
                    zoneCounts[zoneNum] = 0;
                zoneCounts[zoneNum]++;
            }

            string zoneBreakdown = string.Join(", ", 
                zoneCounts.OrderBy(kv => kv.Key)
                          .Select(kv => $"Z{kv.Key}:{kv.Value}"));

            statusLabel.Text = $"🐕 Customer Queue - {customerCount} customer{(customerCount != 1 ? "s" : "")} " +
                              $"waiting - {zoneBreakdown} - Double-click to remove";
        }

        private void FlashWindow()
        {
            try
            {
                this.TopMost = true;
                this.TopMost = false;
            }
            catch { }
        }
    }

    // Queue Item class with zone tracking
    public class QueueItem
    {
        public DateTime ArrivalTime { get; set; }
        public DateTime LastUpdate { get; set; }
        public TagInfo TagInfo { get; set; }
        public List<ZoneVisit> ZoneHistory { get; set; }
    }

    public class ZoneVisit
    {
        public string ZoneName { get; set; }
        public int ZoneNumber { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
