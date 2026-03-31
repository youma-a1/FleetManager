using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using MySql.Data.MySqlClient;
using Microsoft.Win32;
using FleetManager.Helpers;

namespace FleetManager
{
    public partial class AddVehicleWindow : Window
    {
        private string connectionString;
        private string selectedImagePath;

        public AddVehicleWindow(string connStr)
        {
            InitializeComponent();
            connectionString = connStr;
        }

        // 🔹 Parcourir et sélectionner une image
        private void BrowseImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Sélectionner une image",
                Filter = "Images (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|Tous les fichiers (*.*)|*.*",
                FilterIndex = 1
            };

            if (openFileDialog.ShowDialog() == true)
            {
                selectedImagePath = openFileDialog.FileName;
                ImagePathTextBox.Text = Path.GetFileName(selectedImagePath);

                // Affiche l'aperçu de l'image
                try
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(selectedImagePath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    PreviewImage.Source = bitmap;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors du chargement de l'image : {ex.Message}", "Erreur");
                }
            }
        }

        // 🔹 Ajouter le véhicule
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            // 🔹 Validation des champs
            string marque = MarqueTextBox.Text.Trim();
            string modele = ModeleTextBox.Text.Trim();
            string immatriculation = ImmatriculationTextBox.Text.Trim();
            string carburant = ((System.Windows.Controls.ComboBoxItem)CarburantComboBox.SelectedItem).Content.ToString();

            if (string.IsNullOrEmpty(marque) || string.IsNullOrEmpty(modele) || string.IsNullOrEmpty(immatriculation))
            {
                ErrorTextBlock.Text = "⚠️ Marque, modèle et immatriculation sont obligatoires !";
                return;
            }

            // Validation kilométrage
            if (!double.TryParse(KilometrageTextBox.Text, out double kilometrage) || kilometrage < 0)
            {
                ErrorTextBlock.Text = "⚠️ Kilométrage invalide !";
                return;
            }

            // Validation consommation
            if (!double.TryParse(ConsommationTextBox.Text, out double consommation) || consommation < 0)
            {
                ErrorTextBlock.Text = "⚠️ Consommation invalide !";
                return;
            }

            DateTime? dateAchat = DateAchatPicker.SelectedDate;
            if (dateAchat == null)
            {
                ErrorTextBlock.Text = "⚠️ Veuillez sélectionner une date d'achat !";
                return;
            }

            try
            {
                string imageUrl = null;

                // 🔹 Upload de l'image si sélectionnée
                if (!string.IsNullOrEmpty(selectedImagePath))
                {
                    if (AppConfig.UseLocalStorage)
                    {
                        // 🔹 Stockage local
                        imageUrl = SaveImageLocally(selectedImagePath);
                    }
                    else
                    {
                        // 🔹 Upload FTP
                        FtpHelper ftpHelper = new FtpHelper(
                            AppConfig.FtpServer,
                            AppConfig.FtpUsername,
                            AppConfig.FtpPassword,
                            AppConfig.FtpImagePath
                        );

                        string fileName = Path.GetFileName(selectedImagePath);
                        imageUrl = ftpHelper.UploadImage(selectedImagePath, fileName);
                    }
                }

                // 🔹 Insertion dans la base de données
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // Vérifie si l'immatriculation existe déjà
                    string checkQuery = "SELECT COUNT(*) FROM vehicules WHERE immatriculation = @Immatriculation";
                    using (var checkCmd = new MySqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@Immatriculation", immatriculation);
                        int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                        if (count > 0)
                        {
                            ErrorTextBlock.Text = "❌ Cette immatriculation existe déjà !";
                            return;
                        }
                    }

                    // Insère le véhicule
                    string insertQuery = @"INSERT INTO vehicules 
                        (marque, modele, immatriculation, carburant, kilometrage, 
                         consommation_moyenne, date_achat, statut, image_url, agent_id, date_ajout)
                        VALUES 
                        (@Marque, @Modele, @Immatriculation, @Carburant, @Kilometrage, 
                         @Consommation, @DateAchat, 'actif', @ImageUrl, @AgentId, NOW())";

                    using (var cmd = new MySqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@Marque", marque);
                        cmd.Parameters.AddWithValue("@Modele", modele);
                        cmd.Parameters.AddWithValue("@Immatriculation", immatriculation);
                        cmd.Parameters.AddWithValue("@Carburant", carburant);
                        cmd.Parameters.AddWithValue("@Kilometrage", kilometrage);
                        cmd.Parameters.AddWithValue("@Consommation", consommation);
                        cmd.Parameters.AddWithValue("@DateAchat", dateAchat.Value);
                        cmd.Parameters.AddWithValue("@ImageUrl", (object)imageUrl ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@AgentId", CurrentUser.Id);

                        cmd.ExecuteNonQuery();
                    }

                    // Récupère l'ID du véhicule ajouté
                    long vehicleId = 0;
                    string getIdQuery = "SELECT LAST_INSERT_ID()";
                    using (var getIdCmd = new MySqlCommand(getIdQuery, conn))
                    {
                        vehicleId = Convert.ToInt64(getIdCmd.ExecuteScalar());
                    }

                    // Enregistre dans l'historique
                    string historyQuery = @"INSERT INTO historique_vehicules 
                        (vehicule_id, agent_id, action, description, date_modification)
                        VALUES (@VehicleId, @AgentId, 'Création', @Description, NOW())";

                    using (var histCmd = new MySqlCommand(historyQuery, conn))
                    {
                        histCmd.Parameters.AddWithValue("@VehicleId", vehicleId);
                        histCmd.Parameters.AddWithValue("@AgentId", CurrentUser.Id);
                        histCmd.Parameters.AddWithValue("@Description",
                            $"Véhicule créé : {marque} {modele} ({immatriculation})");
                        histCmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show(
                    $"✅ Véhicule ajouté avec succès !\n\n" +
                    $"Marque : {marque}\n" +
                    $"Modèle : {modele}\n" +
                    $"Immatriculation : {immatriculation}\n" +
                    $"Kilométrage : {kilometrage:N0} km",
                    "Succès",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                ErrorTextBlock.Text = $"❌ Erreur : {ex.Message}";
            }
        }

        // 🔹 Sauvegarde l'image localement
        private string SaveImageLocally(string sourcePath)
        {
            try
            {
                // Crée le dossier s'il n'existe pas
                if (!Directory.Exists(AppConfig.LocalImagePath))
                {
                    Directory.CreateDirectory(AppConfig.LocalImagePath);
                }

                // Génère un nom unique
                string fileName = $"{DateTime.Now:yyyyMMddHHmmss}_{Path.GetFileName(sourcePath)}";
                string destPath = Path.Combine(AppConfig.LocalImagePath, fileName);

                // Copie le fichier
                File.Copy(sourcePath, destPath, true);

                // Retourne le chemin relatif ou absolu
                return destPath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur lors de la sauvegarde locale : {ex.Message}");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}