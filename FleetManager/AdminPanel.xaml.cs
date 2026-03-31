using System;
using System.Collections.Generic;
using System.Windows;
using MySql.Data.MySqlClient;

namespace FleetManager
{
    public partial class AdminPanel : Window
    {
        private readonly string connectionString;

        public AdminPanel(string connStr)
        {
            InitializeComponent();
            connectionString = connStr;

            AdminNameText.Text = $"Connecté : {CurrentUser.Prenom} {CurrentUser.Nom} ({CurrentUser.Role})";
            LoadData();
        }

        private void LoadData()
        {
            LoadUsers();
            LoadStatistics();
        }

        // =====================================================
        //  🔹 Chargement des utilisateurs
        // =====================================================
        private void LoadUsers()
        {
            try
            {
                var users = new List<UserInfo>();

                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"SELECT id, nom, prenom, email, role, statut, 
                                    date_inscription, derniere_connexion 
                                    FROM utilisateurs 
                                    ORDER BY id DESC";

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
                                Statut = reader.GetString("statut"),
                                DateInscription = reader.GetDateTime("date_inscription"),
                                DerniereConnexion = reader.IsDBNull(reader.GetOrdinal("derniere_connexion"))
                                    ? (DateTime?)null
                                    : reader.GetDateTime("derniere_connexion")
                            });
                        }
                    }
                }

                UsersDataGrid.ItemsSource = users;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des utilisateurs :\n{ex.Message}",
                                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // =====================================================
        //  🔹 Chargement des statistiques
        // =====================================================
        private void LoadStatistics()
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // Total utilisateurs
                    string totalQuery = "SELECT COUNT(*) FROM utilisateurs";
                    using (var cmd = new MySqlCommand(totalQuery, conn))
                    {
                        TotalUsersText.Text = cmd.ExecuteScalar()?.ToString() ?? "0";
                    }

                    // Agents
                    string agentsQuery = "SELECT COUNT(*) FROM utilisateurs WHERE role = 'agent'";
                    using (var cmd = new MySqlCommand(agentsQuery, conn))
                    {
                        AgentsText.Text = cmd.ExecuteScalar()?.ToString() ?? "0";
                    }

                    // Clients actifs
                    string activeQuery = "SELECT COUNT(*) FROM utilisateurs WHERE role = 'client' AND statut = 'actif'";
                    using (var cmd = new MySqlCommand(activeQuery, conn))
                    {
                        ActiveClientsText.Text = cmd.ExecuteScalar()?.ToString() ?? "0";
                    }

                    // Comptes bloqués
                    string blockedQuery = "SELECT COUNT(*) FROM utilisateurs WHERE statut = 'bloque'";
                    using (var cmd = new MySqlCommand(blockedQuery, conn))
                    {
                        BlockedAccountsText.Text = cmd.ExecuteScalar()?.ToString() ?? "0";
                    }
                }
            }
            catch
            {
                // Statistiques non critiques
            }
        }

        // =====================================================
        //  🔹 Gestion des rôles
        // =====================================================
        private void PromoteToAdmin_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is System.Windows.Controls.Button btn && btn.Tag is int userId))
                return;

            ChangeUserRole(userId, "admin");
        }

        private void PromoteToAgent_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is System.Windows.Controls.Button btn && btn.Tag is int userId))
                return;

            ChangeUserRole(userId, "agent");
        }

        private void DemoteToClient_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is System.Windows.Controls.Button btn && btn.Tag is int userId))
                return;

            ChangeUserRole(userId, "client");
        }

        private void ChangeUserRole(int userId, string newRole)
        {
            if (userId == CurrentUser.Id)
            {
                MessageBox.Show("Vous ne pouvez pas modifier votre propre rôle.", "Attention",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // Vérifier le rôle actuel de l'utilisateur cible
                    string checkQuery = "SELECT role FROM utilisateurs WHERE id = @UserId";
                    string currentRole;
                    using (var cmd = new MySqlCommand(checkQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        currentRole = cmd.ExecuteScalar()?.ToString() ?? "";
                    }

                    // Vérifier les permissions
                    if (CurrentUser.Role.ToLower() == "admin")
                    {
                        if (currentRole == "admin" || currentRole == "super_admin")
                        {
                            MessageBox.Show("Un admin ne peut pas modifier un autre admin ou super_admin.",
                                          "Permission refusée", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }

                    if (currentRole == "super_admin" && CurrentUser.Role.ToLower() != "super_admin")
                    {
                        MessageBox.Show("Seul un super_admin peut modifier un autre super_admin.",
                                      "Permission refusée", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Mise à jour du rôle
                    string updateQuery = "UPDATE utilisateurs SET role = @Role WHERE id = @UserId";
                    using (var cmd = new MySqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@Role", newRole);
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.ExecuteNonQuery();
                    }

                    // Enregistrer dans l'historique (si la table existe)
                    try
                    {
                        string historyQuery = @"INSERT INTO historique_roles 
                            (utilisateur_modifie_id, modificateur_id, ancien_role, nouveau_role, raison, date_modification)
                            VALUES (@UserId, @ModId, @OldRole, @NewRole, @Raison, NOW())";

                        using (var cmd = new MySqlCommand(historyQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@UserId", userId);
                            cmd.Parameters.AddWithValue("@ModId", CurrentUser.Id);
                            cmd.Parameters.AddWithValue("@OldRole", currentRole);
                            cmd.Parameters.AddWithValue("@NewRole", newRole);
                            cmd.Parameters.AddWithValue("@Raison", $"Modification de rôle par {CurrentUser.Prenom} {CurrentUser.Nom}");
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch
                    {
                        // Table historique_roles n'existe pas encore, on ignore
                    }
                }

                MessageBox.Show($"Rôle modifié avec succès vers : {newRole}", "Succès",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la modification du rôle :\n{ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // =====================================================
        //  🔹 Bloquer / Débloquer utilisateurs
        // =====================================================
        private void BlockUser_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is System.Windows.Controls.Button btn && btn.Tag is int userId))
                return;

            ChangeUserStatus(userId, "bloque");
        }

        private void UnblockUser_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is System.Windows.Controls.Button btn && btn.Tag is int userId))
                return;

            ChangeUserStatus(userId, "actif");
        }

        private void ChangeUserStatus(int userId, string newStatus)
        {
            if (userId == CurrentUser.Id)
            {
                MessageBox.Show("Vous ne pouvez pas modifier votre propre statut.", "Attention",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "UPDATE utilisateurs SET statut = @Status WHERE id = @UserId";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Status", newStatus);
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.ExecuteNonQuery();
                    }
                }

                string message = newStatus == "bloque" ? "bloqué" : "débloqué";
                MessageBox.Show($"Utilisateur {message} avec succès.", "Succès",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur :\n{ex.Message}", "Erreur",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // =====================================================
        //  🔹 Boutons d'actions
        // =====================================================
        private void AddUserButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Fonctionnalité en cours de développement", "Information",
                          MessageBoxButton.OK, MessageBoxImage.Information);

            /* À DÉCOMMENTER QUAND AddUserWindow SERA CRÉÉ
            try
            {
                var win = new AddUserWindow(connectionString) { Owner = this };
                if (win.ShowDialog() == true)
                    LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            */
        }

        private void ShowStatistics_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var win = new StatisticsWindow(connectionString) { Owner = this };
                win.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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
                    string query = "DELETE FROM connexions_actives WHERE utilisateur_id = @Id";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", CurrentUser.Id);
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
    //  🔹 Modèle utilisateur
    // =====================================================
    public class UserInfo
    {
        public int Id { get; set; }
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string Statut { get; set; }
        public DateTime DateInscription { get; set; }
        public DateTime? DerniereConnexion { get; set; }
    }
}