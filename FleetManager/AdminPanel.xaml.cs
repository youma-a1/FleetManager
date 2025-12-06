using System;
using System.Collections.Generic;
using System.Windows;
using MySql.Data.MySqlClient;

namespace FleetManager
{
    public partial class AdminPanel : Window
    {
        private string connectionString;

        public AdminPanel(string connStr)
        {
            InitializeComponent();
            connectionString = AppConfig.ConnectionString; // ✅ Utilise la config centralisée
            AdminNameText.Text = $"Connecté : {CurrentUser.Prenom} {CurrentUser.Nom}";
            LoadUsers();
        }
        private void LoadUsers()
        {
            try
            {
                var users = new List<UserInfo>();

                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT id, nom, prenom, email, role, statut FROM utilisateurs ORDER BY id";

                    using (var cmd = new MySqlCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            users.Add(new UserInfo
                            {
                                Id = reader.GetInt32("id"),
                                Nom = reader.GetString("nom"),
                                Prenom = reader.GetString("prenom"),
                                Email = reader.GetString("email"),
                                Role = reader.GetString("role"),
                                Statut = reader.GetString("statut")
                            });
                        }
                    }
                }

                UsersDataGrid.ItemsSource = users;
                UpdateStatistics(users);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement : {ex.Message}", "Erreur");
            }
        }

        private void UpdateStatistics(List<UserInfo> users)
        {
            TotalUsersText.Text = users.Count.ToString();

            int agents = 0;
            int activeClients = 0;
            int blockedUsers = 0;

            foreach (var user in users)
            {
                if (user.Role.ToLower() == "agent")
                    agents++;

                if (user.Role.ToLower() == "client" && user.Statut == "actif")
                    activeClients++;

                if (user.Statut == "bloque")
                    blockedUsers++;
            }

            AgentsText.Text = agents.ToString();
            ActiveClientsText.Text = activeClients.ToString();
            BlockedUsersText.Text = blockedUsers.ToString();
        }

        // 🔹 Ajouter un nouvel utilisateur
        private void AddUser_Click(object sender, RoutedEventArgs e)
        {
            AddUserWindow addUserWindow = new AddUserWindow(connectionString);
            addUserWindow.Owner = this;

            if (addUserWindow.ShowDialog() == true)
            {
                LoadUsers();
            }
        }

        // 🔹 Promouvoir en admin
        private void PromoteToAdmin_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            int userId = (int)button.Tag;

            var result = MessageBox.Show(
                "Promouvoir cet utilisateur en administrateur ?",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                UpdateUserRole(userId, "admin");
            }
        }

        // 🔹 Promouvoir en agent
        private void PromoteToAgent_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            int userId = (int)button.Tag;

            var result = MessageBox.Show(
                "Changer le rôle de cet utilisateur en agent ?",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                UpdateUserRole(userId, "agent");
            }
        }

        // 🔹 Rétrograder en client
        private void DemoteToClient_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            int userId = (int)button.Tag;

            if (userId == CurrentUser.Id)
            {
                MessageBox.Show("Tu ne peux pas te rétrograder toi-même !", "Attention");
                return;
            }

            var result = MessageBox.Show(
                "Rétrograder cet utilisateur en client ?",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                UpdateUserRole(userId, "client");
            }
        }

        // 🔹 Bloquer un utilisateur
        private void BlockUser_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            int userId = (int)button.Tag;

            if (userId == CurrentUser.Id)
            {
                MessageBox.Show("Tu ne peux pas te bloquer toi-même !", "Attention");
                return;
            }

            var result = MessageBox.Show(
                "Bloquer cet utilisateur ?\n\n⚠️ Il ne pourra plus se connecter (licencié/démissionnaire).",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (result == MessageBoxResult.Yes)
            {
                UpdateUserStatus(userId, "bloque");
            }
        }

        // 🔹 Débloquer un utilisateur
        private void UnblockUser_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            int userId = (int)button.Tag;

            UpdateUserStatus(userId, "actif");
        }

        private void UpdateUserRole(int userId, string newRole)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "UPDATE utilisateurs SET role = @Role WHERE id = @Id";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Role", newRole);
                        cmd.Parameters.AddWithValue("@Id", userId);
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show($"✅ Rôle modifié en '{newRole}' avec succès !", "Succès");
                LoadUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur");
            }
        }

        private void UpdateUserStatus(int userId, string newStatus)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "UPDATE utilisateurs SET statut = @Statut WHERE id = @Id";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Statut", newStatus);
                        cmd.Parameters.AddWithValue("@Id", userId);
                        cmd.ExecuteNonQuery();
                    }
                }

                string message = newStatus == "bloque" ? "🔒 Utilisateur bloqué" : "✅ Utilisateur débloqué";
                MessageBox.Show(message, "Succès");
                LoadUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur");
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadUsers();
        }

        private void ViewShop_Click(object sender, RoutedEventArgs e)
        {
            ShopWindow shop = new ShopWindow(connectionString);
            shop.Show();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            // Supprime la connexion active
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "DELETE FROM connexions_actives WHERE utilisateur_id = @UserId";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", CurrentUser.Id);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }

            MainWindow login = new MainWindow();
            login.Show();
            this.Close();
        }
    }

    public class UserInfo
    {
        public int Id { get; set; }
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string Statut { get; set; }
    }
}