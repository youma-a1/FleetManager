using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using MySql.Data.MySqlClient;

namespace FleetManager
{
    public partial class AgentPanel : Window
    {
        private readonly string connectionString;
        private FlotteVehicules flotte;

        public AgentPanel(string connStr)
        {
            InitializeComponent();
            connectionString = connStr;
            flotte = new FlotteVehicules(connectionString);

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

                if (win.ShowDialog() == true)
                {
                    LoadVehicles();
                    flotte = new FlotteVehicules(connectionString);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}",
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

                    if (win.ShowDialog() == true)
                    {
                        LoadVehicles();
                        flotte = new FlotteVehicules(connectionString);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}",
                                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateKm_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Fonctionnalité en cours de développement", "Information");
        }

        private void DeleteVehicle_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is System.Windows.Controls.Button btn &&
                btn.Tag is int vehicleId))
                return;

            string immatriculation = "";
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT immatriculation FROM vehicules WHERE id = @id";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", vehicleId);
                        immatriculation = cmd.ExecuteScalar()?.ToString() ?? "";
                    }
                }
            }
            catch { }

            var confirm = MessageBox.Show(
                $"⚠️ Supprimer définitivement le véhicule {immatriculation} ?\nCette action est irréversible.",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                bool success = flotte.Supprimer(immatriculation);

                if (success)
                {
                    MessageBox.Show("✅ Véhicule supprimé avec succès.", "Succès");
                    LoadVehicles();
                    flotte = new FlotteVehicules(connectionString);
                }
                else
                {
                    MessageBox.Show("❌ Erreur lors de la suppression.", "Erreur");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la suppression :\n{ex.Message}",
                                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // =====================================================
        //  🔹 FONCTIONNALITÉS FLOTTE VÉHICULES (EXERCICE)
        // =====================================================

        // AJOUTER via FlotteVehicules (INSERT SQL)
        private void AjouterFlotte_Click(object sender, RoutedEventArgs e)
        {
            // Fenêtre de saisie pour la marque
            var dialogMarque = new InputDialog("Entrez la marque :", "Ajouter un véhicule", "Renault");
            if (dialogMarque.ShowDialog() != true || string.IsNullOrWhiteSpace(dialogMarque.ResponseText))
                return;
            string marque = dialogMarque.ResponseText.Trim();

            // Fenêtre de saisie pour le modèle
            var dialogModele = new InputDialog("Entrez le modèle :", "Ajouter un véhicule", "Clio");
            if (dialogModele.ShowDialog() != true || string.IsNullOrWhiteSpace(dialogModele.ResponseText))
                return;
            string modele = dialogModele.ResponseText.Trim();

            // Fenêtre de saisie pour l'immatriculation
            var dialogImmat = new InputDialog("Entrez l'immatriculation :", "Ajouter un véhicule", "AB-123-CD");
            if (dialogImmat.ShowDialog() != true || string.IsNullOrWhiteSpace(dialogImmat.ResponseText))
                return;
            string immatriculation = dialogImmat.ResponseText.Trim().ToUpper();

            // Fenêtre de saisie pour le carburant
            var dialogCarburant = new InputDialog("Entrez le carburant (Essence, Diesel, Électrique, Hybride, GPL) :", "Ajouter un véhicule", "Essence");
            if (dialogCarburant.ShowDialog() != true || string.IsNullOrWhiteSpace(dialogCarburant.ResponseText))
                return;
            string carburant = dialogCarburant.ResponseText.Trim();

            // Fenêtre de saisie pour le kilométrage
            var dialogKm = new InputDialog("Entrez le kilométrage :", "Ajouter un véhicule", "50000");
            if (dialogKm.ShowDialog() != true || !int.TryParse(dialogKm.ResponseText, out int kilometrage))
            {
                MessageBox.Show("❌ Le kilométrage doit être un nombre valide.", "Erreur");
                return;
            }

            // Créer le véhicule
            var vehicule = new VehiculeFlotte
            {
                Marque = marque,
                Modele = modele,
                Immatriculation = immatriculation,
                Carburant = carburant,
                Kilometrage = kilometrage,
                Statut = "actif"
            };

            // Ajouter via INSERT SQL paramétré
            bool success = flotte.Ajouter(vehicule);

            if (success)
            {
                MessageBox.Show(
                    $"✅ Véhicule ajouté avec succès via INSERT SQL !\n\n" +
                    $"Marque : {vehicule.Marque}\n" +
                    $"Modèle : {vehicule.Modele}\n" +
                    $"Immatriculation : {vehicule.Immatriculation}",
                    "Succès");

                LoadVehicles();
                flotte = new FlotteVehicules(connectionString);
            }
            else
            {
                MessageBox.Show("❌ Erreur lors de l'ajout du véhicule.", "Erreur");
            }
        }

        // SUPPRIMER via FlotteVehicules (DELETE SQL)
        private void SupprimerFlotte_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new InputDialog(
                "Entrez l'immatriculation du véhicule à supprimer :",
                "Suppression via FlotteVehicules",
                "");

            if (dialog.ShowDialog() == true)
            {
                string immat = dialog.ResponseText;

                if (!string.IsNullOrEmpty(immat))
                {
                    var confirm = MessageBox.Show(
                        $"⚠️ Supprimer définitivement le véhicule {immat} via DELETE SQL ?\nCette action est irréversible.",
                        "Confirmation",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (confirm == MessageBoxResult.Yes)
                    {
                        bool success = flotte.Supprimer(immat);

                        if (success)
                        {
                            MessageBox.Show("✅ Véhicule supprimé avec succès via DELETE SQL !", "Succès");
                            LoadVehicles();
                            flotte = new FlotteVehicules(connectionString);
                        }
                        else
                        {
                            MessageBox.Show("❌ Erreur lors de la suppression ou véhicule introuvable.", "Erreur");
                        }
                    }
                }
            }
        }

        // RECHERCHER par immatriculation
        private void RechercherParImmat_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new InputDialog(
                "Entrez l'immatriculation à rechercher :",
                "Recherche par immatriculation",
                "");

            if (dialog.ShowDialog() == true)
            {
                string immat = dialog.ResponseText;

                if (!string.IsNullOrEmpty(immat))
                {
                    var vehicule = flotte.RechercherParImmatriculation(immat);
                    if (vehicule != null)
                    {
                        MessageBox.Show(
                            $"✅ Véhicule trouvé !\n\n" +
                            $"Marque : {vehicule.Marque}\n" +
                            $"Modèle : {vehicule.Modele}\n" +
                            $"Immatriculation : {vehicule.Immatriculation}\n" +
                            $"Carburant : {vehicule.Carburant}\n" +
                            $"Kilométrage : {vehicule.Kilometrage} km\n" +
                            $"Statut : {vehicule.Statut}",
                            "Véhicule trouvé",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        var singleVehicle = new List<VehicleInfo>
                        {
                            new VehicleInfo
                            {
                                Id = vehicule.Id,
                                Marque = vehicule.Marque,
                                Modele = vehicule.Modele,
                                Immatriculation = vehicule.Immatriculation,
                                Carburant = vehicule.Carburant,
                                Kilometrage = vehicule.Kilometrage,
                                Statut = vehicule.Statut
                            }
                        };
                        VehiclesDataGrid.ItemsSource = singleVehicle;
                    }
                    else
                    {
                        MessageBox.Show($"❌ Aucun véhicule trouvé avec l'immatriculation : {immat}",
                                      "Recherche", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
        }

        // RECHERCHER par marque
        private void RechercherParMarque_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new InputDialog(
                "Entrez la marque à rechercher :",
                "Recherche par marque",
                "");

            if (dialog.ShowDialog() == true)
            {
                string marque = dialog.ResponseText;

                if (!string.IsNullOrEmpty(marque))
                {
                    var resultats = flotte.RechercherParMarque(marque);

                    if (resultats.Count > 0)
                    {
                        var vehiclesList = new List<VehicleInfo>();
                        foreach (var v in resultats)
                        {
                            vehiclesList.Add(new VehicleInfo
                            {
                                Id = v.Id,
                                Marque = v.Marque,
                                Modele = v.Modele,
                                Immatriculation = v.Immatriculation,
                                Carburant = v.Carburant,
                                Kilometrage = v.Kilometrage,
                                Statut = v.Statut
                            });
                        }
                        VehiclesDataGrid.ItemsSource = vehiclesList;

                        MessageBox.Show($"✅ {resultats.Count} véhicule(s) trouvé(s) pour la marque : {marque}",
                                      "Résultats", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show($"❌ Aucun véhicule trouvé pour la marque : {marque}",
                                      "Recherche", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
        }

        // RECHERCHER par carburant
        private void RechercherParCarburant_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new InputDialog(
                "Entrez le type de carburant :\n(Essence, Diesel, Électrique, Hybride, GPL)",
                "Recherche par carburant",
                "Essence");

            if (dialog.ShowDialog() == true)
            {
                string carburant = dialog.ResponseText;

                if (!string.IsNullOrEmpty(carburant))
                {
                    var resultats = flotte.RechercherParTypeEnergie(carburant);

                    if (resultats.Count > 0)
                    {
                        var vehiclesList = new List<VehicleInfo>();
                        foreach (var v in resultats)
                        {
                            vehiclesList.Add(new VehicleInfo
                            {
                                Id = v.Id,
                                Marque = v.Marque,
                                Modele = v.Modele,
                                Immatriculation = v.Immatriculation,
                                Carburant = v.Carburant,
                                Kilometrage = v.Kilometrage,
                                Statut = v.Statut
                            });
                        }
                        VehiclesDataGrid.ItemsSource = vehiclesList;

                        MessageBox.Show($"✅ {resultats.Count} véhicule(s) {carburant} trouvé(s)",
                                      "Résultats", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show($"❌ Aucun véhicule {carburant} trouvé",
                                      "Recherche", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
        }

        // RECHERCHER par kilométrage max
        private void RechercherParKm_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new InputDialog(
                "Entrez le kilométrage maximum :",
                "Recherche par kilométrage",
                "100000");

            if (dialog.ShowDialog() == true)
            {
                string input = dialog.ResponseText;

                if (!string.IsNullOrEmpty(input) && int.TryParse(input, out int kmMax))
                {
                    var resultats = flotte.RechercherParKilometrageMax(kmMax);

                    if (resultats.Count > 0)
                    {
                        var vehiclesList = new List<VehicleInfo>();
                        foreach (var v in resultats)
                        {
                            vehiclesList.Add(new VehicleInfo
                            {
                                Id = v.Id,
                                Marque = v.Marque,
                                Modele = v.Modele,
                                Immatriculation = v.Immatriculation,
                                Carburant = v.Carburant,
                                Kilometrage = v.Kilometrage,
                                Statut = v.Statut
                            });
                        }
                        VehiclesDataGrid.ItemsSource = vehiclesList;

                        MessageBox.Show($"✅ {resultats.Count} véhicule(s) avec kilométrage ≤ {kmMax} km",
                                      "Résultats", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show($"❌ Aucun véhicule trouvé avec kilométrage ≤ {kmMax} km",
                                      "Recherche", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
        }

        // =====================================================
        //  🔹 Fenêtres diverses
        // =====================================================
        private void ShowStatistics_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var win = new StatisticsWindow(connectionString) { Owner = this };
                win.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}",
                                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // =====================================================
        //  🔹 Actualiser / Déconnexion
        // =====================================================
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
            flotte = new FlotteVehicules(connectionString);
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