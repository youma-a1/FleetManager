using System;
using System.Windows;
using System.Windows.Media;
using MySql.Data.MySqlClient;

namespace FleetManager
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            CleanOldConnections();
        }

        // 🔹 Affiche un message avec MessageBox au lieu de contrôles XAML
        private void ShowMessage(string message, MessageType type)
        {
            MessageBoxImage icon;
            string title;

            switch (type)
            {
                case MessageType.Success:
                    icon = MessageBoxImage.Information;
                    title = "Succès";
                    break;
                case MessageType.Error:
                    icon = MessageBoxImage.Error;
                    title = "Erreur";
                    break;
                case MessageType.Warning:
                    icon = MessageBoxImage.Warning;
                    title = "Attention";
                    break;
                default:
                    icon = MessageBoxImage.None;
                    title = "Information";
                    break;
            }

            MessageBox.Show(message, title, MessageBoxButton.OK, icon);
        }

        // 🔹 Cache le message (non utilisé avec MessageBox, mais gardé pour compatibilité)
        private void HideMessage()
        {
            // Méthode vide - pas nécessaire avec MessageBox
        }

        // 🔹 Nettoie les connexions de plus de 24h
        private void CleanOldConnections()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(AppConfig.ConnectionString))
                {
                    conn.Open();
                    string query = "DELETE FROM connexions_actives WHERE date_connexion < DATE_SUB(NOW(), INTERVAL 24 HOUR)";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }
        }

        // 🔹 Réinitialise le mot de passe
        private void ResetPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string email = EmailTextBox.Text.Trim();
                if (string.IsNullOrEmpty(email))
                {
                    ShowMessage("⚠️ Veuillez entrer un email d'abord !", MessageType.Warning);
                    return;
                }

                string newPassword = "admin123";
                string newHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

                using (MySqlConnection conn = new MySqlConnection(AppConfig.ConnectionString))
                {
                    conn.Open();
                    string query = "UPDATE utilisateurs SET mot_de_passe = @Hash WHERE email = @Email";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Hash", newHash);
                        cmd.Parameters.AddWithValue("@Email", email);

                        int rows = cmd.ExecuteNonQuery();

                        if (rows > 0)
                        {
                            MessageBox.Show($"✅ Mot de passe réinitialisé avec succès !\n\n📧 Email: {email}\n🔑 Nouveau mot de passe: {newPassword}",
                                          "Réinitialisation réussie",
                                          MessageBoxButton.OK,
                                          MessageBoxImage.Information);
                            PasswordBox.Clear();
                        }
                        else
                        {
                            ShowMessage("❌ Aucun utilisateur trouvé avec cet email.", MessageType.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"❌ Erreur : {ex.Message}", MessageType.Error);
            }
        }

        // 🔹 Connexion
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            HideMessage();

            string email = EmailTextBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ShowMessage("⚠️ Veuillez remplir tous les champs.", MessageType.Warning);
                return;
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(AppConfig.ConnectionString))
                {
                    conn.Open();
                    string query = "SELECT id, nom, prenom, role, mot_de_passe, statut FROM utilisateurs WHERE email=@Email LIMIT 1";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Email", email);

                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                ShowMessage("❌ Email ou mot de passe incorrect.", MessageType.Error);
                                return;
                            }

                            string statut = reader.GetString("statut");
                            if (statut == "bloque")
                            {
                                ShowMessage("🚫 Votre compte a été bloqué. Contactez l'administrateur.", MessageType.Error);
                                return;
                            }

                            string storedHash = reader.GetString("mot_de_passe").Trim();
                            bool isValid = false;

                            try
                            {
                                isValid = BCrypt.Net.BCrypt.Verify(password, storedHash);
                            }
                            catch
                            {
                                ShowMessage("⚠️ Format de mot de passe invalide. Utilisez 'Réinitialiser le mot de passe'", MessageType.Warning);
                                return;
                            }

                            if (!isValid)
                            {
                                ShowMessage("❌ Email ou mot de passe incorrect.", MessageType.Error);
                                return;
                            }

                            // 🔹 Stocke les infos utilisateur
                            CurrentUser.Id = reader.GetInt32("id");
                            CurrentUser.Nom = reader.GetString("nom");
                            CurrentUser.Prenom = reader.GetString("prenom");
                            CurrentUser.Role = reader.GetString("role");
                            CurrentUser.Statut = statut;
                            CurrentUser.Email = email;
                        }
                    }

                    // 🔹 Enregistre la connexion
                    string insertConn = "INSERT INTO connexions_actives (utilisateur_id) VALUES (@UserId)";
                    using (MySqlCommand cmd = new MySqlCommand(insertConn, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", CurrentUser.Id);
                        cmd.ExecuteNonQuery();
                    }

                    // 🔹 Met à jour la dernière connexion
                    string updateLastLogin = "UPDATE utilisateurs SET derniere_connexion = NOW() WHERE id = @Id";
                    using (MySqlCommand cmd = new MySqlCommand(updateLastLogin, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", CurrentUser.Id);
                        cmd.ExecuteNonQuery();
                    }
                }

                // 🔹 Redirige selon le rôle
                Window nextWindow = null;

                switch (CurrentUser.Role.ToLower())
                {
                    case "super_admin":
                    case "admin":
                        nextWindow = new AdminPanel(AppConfig.ConnectionString);
                        break;
                    case "agent":
                        nextWindow = new AgentPanel(AppConfig.ConnectionString);
                        break;
                    case "client":
                        nextWindow = new ShopWindow(AppConfig.ConnectionString);
                        break;
                    default:
                        ShowMessage($"❌ Rôle non reconnu : '{CurrentUser.Role}'", MessageType.Error);
                        return;
                }

                MessageBox.Show("✅ Connexion réussie ! Bienvenue.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);

                nextWindow.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                ShowMessage($"❌ Erreur de connexion : {ex.Message}", MessageType.Error);
            }
        }

        private void BoutiqueImage_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Action sur clic image (optionnel)
        }
    }

    // 🔹 Enum pour les types de messages
    public enum MessageType
    {
        Success,
        Error,
        Warning
    }

    // 🔹 Classe utilisateur pour stocker les infos de session
    public static class CurrentUser
    {
        public static int Id { get; set; }
        public static string Nom { get; set; }
        public static string Prenom { get; set; }
        public static string Email { get; set; }
        public static string Role { get; set; }
        public static string Statut { get; set; }
    }
}