using System;
using System.Windows;
using MySql.Data.MySqlClient;

namespace FleetManager
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // Nettoie les anciennes connexions au démarrage
            CleanOldConnections();
        }

        // 🔹 Nettoie les connexions de plus de 24h
        private void CleanOldConnections()
        {
            try
            {
                using (var conn = new MySqlConnection(AppConfig.ConnectionString))
                {
                    conn.Open();
                    string query = "DELETE FROM connexions_actives WHERE date_connexion < DATE_SUB(NOW(), INTERVAL 24 HOUR)";
                    using (var cmd = new MySqlCommand(query, conn))
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
                    MessageBox.Show("Entre un email d'abord !", "Attention");
                    return;
                }

                string newPassword = "Compac?123000";
                string newHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

                using (var conn = new MySqlConnection(AppConfig.ConnectionString))
                {
                    conn.Open();
                    string query = "UPDATE utilisateurs SET mot_de_passe = @Hash WHERE email = @Email";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Hash", newHash);
                        cmd.Parameters.AddWithValue("@Email", email);

                        int rows = cmd.ExecuteNonQuery();

                        if (rows > 0)
                        {
                            MessageBox.Show($"✅ Mot de passe réinitialisé !\n\nEmail: {email}\nMot de passe: {newPassword}", "Succès");
                        }
                        else
                        {
                            MessageBox.Show("❌ Utilisateur non trouvé.", "Erreur");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur");
            }
        }

        // 🔹 Connexion avec enregistrement de la session
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailTextBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ResultTextBlock.Text = "Veuillez remplir tous les champs.";
                ResultTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                return;
            }

            try
            {
                using (var conn = new MySqlConnection(AppConfig.ConnectionString))
                {
                    conn.Open();
                    string query = "SELECT id, nom, prenom, role, mot_de_passe, statut FROM utilisateurs WHERE email=@Email LIMIT 1";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Email", email);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                ResultTextBlock.Text = "Email ou mot de passe incorrect.";
                                ResultTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                                return;
                            }

                            string statut = reader.GetString("statut");
                            if (statut == "bloque")
                            {
                                ResultTextBlock.Text = "⛔ Compte bloqué.";
                                ResultTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                                MessageBox.Show("Ton compte a été bloqué. Contacte l'administrateur.", "Accès refusé");
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
                                ResultTextBlock.Text = "⚠️ Hash invalide ! Utilise 'Réinitialiser mot de passe'";
                                ResultTextBlock.Foreground = System.Windows.Media.Brushes.Orange;
                                return;
                            }

                            if (!isValid)
                            {
                                ResultTextBlock.Text = "Email ou mot de passe incorrect.";
                                ResultTextBlock.Foreground = System.Windows.Media.Brushes.Red;
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
                    using (var cmd = new MySqlCommand(insertConn, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", CurrentUser.Id);
                        cmd.ExecuteNonQuery();
                    }

                    // 🔹 Met à jour la dernière connexion
                    string updateLastLogin = "UPDATE utilisateurs SET derniere_connexion = NOW() WHERE id = @Id";
                    using (var cmd = new MySqlCommand(updateLastLogin, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", CurrentUser.Id);
                        cmd.ExecuteNonQuery();
                    }
                }

                // 🔹 Redirige selon le rôle avec connectionString
                Window nextWindow = null;

                switch (CurrentUser.Role.ToLower())
                {
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
                        MessageBox.Show("Rôle non reconnu !", "Erreur");
                        return;
                }

                nextWindow.Show();
                this.Close();

                nextWindow.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                ResultTextBlock.Text = "Erreur : " + ex.Message;
                ResultTextBlock.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private void BoutiqueImage_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Action sur clic image
        }
    }

    // 🔹 Classe utilisateur étendue
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