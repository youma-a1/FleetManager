using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MySql.Data.MySqlClient;

namespace FleetManager
{
    public partial class ShopWindow : Window
    {
        private string connectionString;
        private List<SimpleVehicle> vehicles;

        public ShopWindow(string connStr)
        {
            InitializeComponent();
            connectionString = connStr;
            ClientNameText.Text = $"Bienvenue, {CurrentUser.Prenom} {CurrentUser.Nom}";
            LoadVehicles();
        }

        private void LoadVehicles()
        {
            try
            {
                vehicles = new List<SimpleVehicle>();

                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT id, marque, modele, immatriculation, carburant, kilometrage, statut FROM vehicules WHERE statut = 'actif'";

                    using (var cmd = new MySqlCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            vehicles.Add(new SimpleVehicle
                            {
                                Id = reader.GetInt32(0),
                                Marque = reader.GetString(1),
                                Modele = reader.GetString(2),
                                Immatriculation = reader.GetString(3),
                                Carburant = reader.GetString(4),
                                Kilometrage = reader.GetInt32(5),
                                Statut = reader.GetString(6)
                            });
                        }
                    }
                }

                VehiclesDataGrid.ItemsSource = vehicles;
                TotalText.Text = $"Total : {vehicles.Count} véhicules";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur");
            }
        }

        private void ViewButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var vehicle = vehicles.FirstOrDefault(v => v.Id == id);
                if (vehicle != null)
                {
                    MessageBox.Show($"Véhicule : {vehicle.Marque} {vehicle.Modele}\nImmatriculation : {vehicle.Immatriculation}\nCarburant : {vehicle.Carburant}\nKilométrage : {vehicle.Kilometrage} km", "Détails");
                }
            }
        }

        private void OrderButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var vehicle = vehicles.FirstOrDefault(v => v.Id == id);
                if (vehicle != null)
                {
                    var result = MessageBox.Show($"Commander : {vehicle.Marque} {vehicle.Modele} ?", "Confirmation", MessageBoxButton.YesNo);

                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            using (var conn = new MySqlConnection(connectionString))
                            {
                                conn.Open();
                                string query = "INSERT INTO commandes (utilisateur_id, vehicule_id, statut, date_commande) VALUES (@uid, @vid, 'en_attente', NOW())";

                                using (var cmd = new MySqlCommand(query, conn))
                                {
                                    cmd.Parameters.AddWithValue("@uid", CurrentUser.Id);
                                    cmd.Parameters.AddWithValue("@vid", id);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            MessageBox.Show("✅ Commande enregistrée !", "Succès");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Erreur : {ex.Message}", "Erreur");
                        }
                    }
                }
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadVehicles();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand("DELETE FROM connexions_actives WHERE utilisateur_id = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", CurrentUser.Id);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }

            new MainWindow().Show();
            Close();
        }
    }

    // Classe simple pour les véhicules
    public class SimpleVehicle
    {
        public int Id { get; set; }
        public string Marque { get; set; }
        public string Modele { get; set; }
        public string Immatriculation { get; set; }
        public string Carburant { get; set; }
        public int Kilometrage { get; set; }
        public string Statut { get; set; }
    }
}