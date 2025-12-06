using System;
using System.Collections.Generic;
using System.Windows;
using MySql.Data.MySqlClient;

namespace FleetManager
{
    public partial class AgentPanel : Window
    {
        private readonly string connectionString;

        public AgentPanel(string connStr)
        {
            InitializeComponent();
            connectionString = connStr;

            AgentNameText.Text = $"Connecté : {CurrentUser.Prenom} {CurrentUser.Nom}";
            LoadData();
        }

        private void LoadData()
        {
            LoadVehicles();
            LoadConnectedClients();
        }

        // =====================================================
        //  🔹 Chargement des véhicules
        // =====================================================
        private void LoadVehicles()
        {
            try
            {
                var vehicles = new List<VehicleInfo>();

                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    string query = @"
                        SELECT id, marque, modele, immatriculation, carburant, 
                               kilometrage, statut, image_url 
                        FROM vehicules 
                        ORDER BY id DESC";

                    using (var cmd = new MySqlCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        // Récupération des index des colonnes
                        int idxId = reader.GetOrdinal("id");
                        int idxMarque = reader.GetOrdinal("marque");
                        int idxModele = reader.GetOrdinal("modele");
                        int idxImmat = reader.GetOrdinal("immatriculation");
                        int idxCarburant = reader.GetOrdinal("carburant");
                        int idxKm = reader.GetOrdinal("kilometrage");
                        int idxStatut = reader.GetOrdinal("statut");
                        int idxImage = reader.GetOrdinal("image_url");

                        while (reader.Read())
                        {
                            vehicles.Add(new VehicleInfo
                            {
                                Id = reader.GetInt32(idxId),
                                Marque = reader.GetString(idxMarque),
                                Modele = reader.GetString(idxModele),
                                Immatriculation = reader.GetString(idxImmat),
                                Carburant = reader.GetString(idxCarburant),
                                Kilometrage = reader.GetDouble(idxKm),
                                Statut = reader.GetString(idxStatut),
                                ImageUrl = reader.IsDBNull(idxImage) ? null : reader.GetString(idxImage)
                            });
                        }
                    }
                }

                VehiclesDataGrid.ItemsSource = vehicles;
                UpdateVehicleStatistics(vehicles);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des véhicules :\n{ex.Message}",
                                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateVehicleStatistics(List<VehicleInfo> vehicles)
        {
            TotalVehiclesText.Text = vehicles.Count.ToString();

            int activeVehicles = 0;

            foreach (var v in vehicles)
            {
                if (v.Statut.Equals("actif", StringComparison.OrdinalIgnoreCase))
                    activeVehicles++;
            }

            ActiveVehiclesText.Text = activeVehicles.ToString();
        }

        // =====================================================
        //  🔹 Clients connectés
        // =====================================================
        private void LoadConnectedClients()
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    string query = @"
                        SELECT COUNT(DISTINCT ca.utilisateur_id) AS connected_count
                        FROM connexions_actives ca
                        JOIN utilisateurs u ON ca.utilisateur_id = u.id
                        WHERE u.role = 'client'
                        AND u.statut = 'actif'
                        AND ca.date_connexion > DATE_SUB(NOW(), INTERVAL 1 HOUR)";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        ConnectedClientsText.Text = cmd.ExecuteScalar()?.ToString() ?? "0";
                    }
                }
            }
            catch
            {
                ConnectedClientsText.Text = "N/A";
            }
        }

        // =====================================================
        //  🔹 CRUD véhicules (Ajouter / Modifier / Supprimer)
        // =====================================================
        private void AddVehicle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var win = new AddVehicleWindow(connectionString) { Owner = this };

                if (win.ShowDialog())
                    LoadVehicles();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}\nVérifiez AddVehicleWindow.",
                                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditVehicle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.Button btn &&
                    btn.Tag is int vehicleId)
                {
                    var win = new EditVehicleWindow(connectionString, vehicleId) { Owner = this };

                    if (win.ShowDialog())
                        LoadVehicles();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}\nVérifiez EditVehicleWindow.",
                                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateKm_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.Button btn &&
                    btn.Tag is int vehicleId)
                {
                    var win = new UpdateKmWindow(connectionString, vehicleId) { Owner = this };

                    if (win.ShowDialog())
                        LoadVehicles();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}\nVérifiez UpdateKmWindow.",
                                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteVehicle_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is System.Windows.Controls.Button btn &&
                btn.Tag is int vehicleId))
                return;

            var confirm = MessageBox.Show(
                "⚠️ Supprimer définitivement ce véhicule ?\nCette action est irréversible.",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    using (var cmd = new MySqlCommand(
                        "DELETE FROM historique_vehicules WHERE vehicule_id = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", vehicleId);
                        cmd.ExecuteNonQuery();
                    }

                    using (var cmd = new MySqlCommand(
                        "DELETE FROM vehicules WHERE id = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", vehicleId);
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Véhicule supprimé avec succès.", "Succès");
                LoadVehicles();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la suppression :\n{ex.Message}",
                                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // =====================================================
        //  🔹 Fenêtres diverses
        // =====================================================
        private void ShowStatistics_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var win = new StatisticsWindow { Owner = this };
                win.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}\nVérifiez StatisticsWindow.",
                                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewClients_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var win = new ConnectedClientsWindow(connectionString) { Owner = this };
                win.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}\nVérifiez ConnectedClientsWindow.",
                                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // =====================================================
        //  🔹 Actualiser / Déconnexion
        // =====================================================
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    using (var cmd = new MySqlCommand(
                        "DELETE FROM connexions_actives WHERE utilisateur_id = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", CurrentUser.Id);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }

            var login = new MainWindow();
            login.Show();
            Close();
        }
    }

    // =====================================================
    //  🔹 FENÊTRES FACTICES (ne pas toucher)
    // =====================================================
    internal class AddVehicleWindow
    {
        public AddVehicleWindow(string connectionString) { }
        public AgentPanel Owner { get; set; }
        public bool ShowDialog() => true;
    }

    internal class EditVehicleWindow
    {
        public EditVehicleWindow(string connectionString, int id) { }
        public AgentPanel Owner { get; set; }
        public bool ShowDialog() => true;
    }

    internal class UpdateKmWindow
    {
        public UpdateKmWindow(string connectionString, int id) { }
        public AgentPanel Owner { get; set; }
        public bool ShowDialog() => true;
    }

    internal class StatisticsWindow
    {
        public AgentPanel Owner { get; set; }
        public void ShowDialog() { }
    }

    internal class ConnectedClientsWindow
    {
        public ConnectedClientsWindow(string connectionString) { }
        public AgentPanel Owner { get; set; }
        public void ShowDialog() { }
    }

    // =====================================================
    //  🔹 Modèle véhicule
    // =====================================================
    public class VehicleInfo
    {
        public int Id { get; set; }
        public string Marque { get; set; }
        public string Modele { get; set; }
        public string Immatriculation { get; set; }
        public string Carburant { get; set; }
        public double Kilometrage { get; set; }
        public string Statut { get; set; }
        public string ImageUrl { get; set; }
    }
}
