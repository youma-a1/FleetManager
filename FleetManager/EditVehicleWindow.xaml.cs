using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using MySql.Data.MySqlClient;
using FleetManager.Helpers;
using Microsoft.Win32;

namespace FleetManager
{
    public partial class EditVehicleWindow : Window
    {
        private string connectionString;
        private int vehicleId;
        private string selectedImagePath;
        private string currentImageUrl;

        public EditVehicleWindow(string connStr, int vId)
        {
            InitializeComponent();
            connectionString = connStr;
            vehicleId = vId;
            LoadVehicleData();
        }

        // 🔹 Charge les données du véhicule
        private void LoadVehicleData()
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"SELECT marque, modele, immatriculation, carburant, 
                                    consommation_moyenne, date_achat, statut, image_url
                                    FROM vehicules WHERE id = @VehicleId";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@VehicleId", vehicleId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                MarqueTextBox.Text = reader.GetString("marque");
                                ModeleTextBox.Text = reader.GetString("modele");
                                ImmatriculationTextBox.Text = reader.GetString("immatriculation");

                                string carburant = reader.GetString("carburant");
                                foreach (System.Windows.Controls.ComboBoxItem item in CarburantComboBox.Items)
                                {
                                    if (item.Content.ToString() == carburant)
                                    {
                                        CarburantComboBox.SelectedItem = item;
                                        break;
                                    }
                                }

                                ConsommationTextBox.Text = reader.GetDouble("consommation_moyenne").ToString();
                                DateAchatPicker.SelectedDate = reader.GetDateTime("date_achat");

                                string statut = reader.GetString("statut");
                                foreach (System.Windows.Controls.ComboBoxItem item in StatutComboBox.Items)
                                {
                                    if (item.Content.ToString() == statut)
                                    {
                                        StatutComboBox.SelectedItem = item;
                                        break;
                                    }
                                }

                                if (!reader.IsDBNull(reader.GetOrdinal("image_url")))
                                {
                                    currentImageUrl = reader.GetString("image_url");
                                    LoadImagePreview(currentImageUrl);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement : {ex.Message}", "Erreur");
            }
        }

        // 🔹 Charge l'aperçu de l'image
        private void LoadImagePreview(string imageUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(imageUrl))
                    return;

                if (File.Exists(imageUrl))
                {
                    // Image locale
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(imageUrl, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    PreviewImage.Source = bitmap;
                }
                else if (Uri.IsWellFormedUriString(imageUrl, UriKind.Absolute))
                {
                    // Image distante (URL)
                    PreviewImage.Source = new BitmapImage(new Uri(imageUrl));
                }
            }
            catch
            {
                // Image non disponible
                PreviewImage.Source = null;
            }
        }

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

                try
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(selectedImagePath, UriKind.Absolute);
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

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Réinitialiser le message d'erreur
            ErrorTextBlock.Text = "";

            string marque = MarqueTextBox.Text.Trim();
            string modele = ModeleTextBox.Text.Trim();
            string immatriculation = ImmatriculationTextBox.Text.Trim();

            if (string.IsNullOrEmpty(marque) || string.IsNullOrEmpty(modele) || string.IsNullOrEmpty(immatriculation))
            {
                ErrorTextBlock.Text = "⚠️ Marque, modèle et immatriculation sont obligatoires !";
                return;
            }

            if (CarburantComboBox.SelectedItem == null)
            {
                ErrorTextBlock.Text = "⚠️ Veuillez sélectionner un carburant !";
                return;
            }

            if (StatutComboBox.SelectedItem == null)
            {
                ErrorTextBlock.Text = "⚠️ Veuillez sélectionner un statut !";
                return;
            }

            string carburant = ((System.Windows.Controls.ComboBoxItem)CarburantComboBox.SelectedItem).Content.ToString();
            string statut = ((System.Windows.Controls.ComboBoxItem)StatutComboBox.SelectedItem).Content.ToString();

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
                string imageUrl = currentImageUrl;

                // Upload nouvelle image si sélectionnée
                if (!string.IsNullOrEmpty(selectedImagePath))
                {
                    if (AppConfig.UseLocalStorage)
                    {
                        imageUrl = SaveImageLocally(selectedImagePath);
                    }
                    else
                    {
                        FtpHelper ftpHelper = new FtpHelper(
                            AppConfig.FtpServer,
                            AppConfig.FtpUsername,
                            AppConfig.FtpPassword,
                            AppConfig.FtpImagePath
                        );
                        imageUrl = ftpHelper.UploadImage(selectedImagePath, Path.GetFileName(selectedImagePath));
                    }
                }

                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    string updateQuery = @"UPDATE vehicules SET 
                        marque = @Marque, 
                        modele = @Modele, 
                        immatriculation = @Immatriculation,
                        carburant = @Carburant,
                        consommation_moyenne = @Consommation,
                        date_achat = @DateAchat,
                        statut = @Statut,
                        image_url = @ImageUrl
                        WHERE id = @VehicleId";

                    using (var cmd = new MySqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@Marque", marque);
                        cmd.Parameters.AddWithValue("@Modele", modele);
                        cmd.Parameters.AddWithValue("@Immatriculation", immatriculation);
                        cmd.Parameters.AddWithValue("@Carburant", carburant);
                        cmd.Parameters.AddWithValue("@Consommation", consommation);
                        cmd.Parameters.AddWithValue("@DateAchat", dateAchat.Value);
                        cmd.Parameters.AddWithValue("@Statut", statut);
                        cmd.Parameters.AddWithValue("@ImageUrl", (object)imageUrl ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@VehicleId", vehicleId);

                        cmd.ExecuteNonQuery();
                    }

                    // Historique
                    string historyQuery = @"INSERT INTO historique_vehicules 
                        (vehicule_id, agent_id, action, description, date_modification)
                        VALUES (@VehicleId, @AgentId, 'Modification', @Description, NOW())";

                    using (var histCmd = new MySqlCommand(historyQuery, conn))
                    {
                        histCmd.Parameters.AddWithValue("@VehicleId", vehicleId);
                        histCmd.Parameters.AddWithValue("@AgentId", CurrentUser.Id);
                        histCmd.Parameters.AddWithValue("@Description", $"Modification du véhicule {marque} {modele}");
                        histCmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("✅ Véhicule modifié avec succès !", "Succès");
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                ErrorTextBlock.Text = $"❌ Erreur : {ex.Message}";
            }
        }

        private string SaveImageLocally(string sourcePath)
        {
            try
            {
                if (!Directory.Exists(AppConfig.LocalImagePath))
                {
                    Directory.CreateDirectory(AppConfig.LocalImagePath);
                }

                string fileName = $"{DateTime.Now:yyyyMMddHHmmss}_{Path.GetFileName(sourcePath)}";
                string destPath = Path.Combine(AppConfig.LocalImagePath, fileName);
                File.Copy(sourcePath, destPath, true);
                return destPath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur sauvegarde locale : {ex.Message}");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}