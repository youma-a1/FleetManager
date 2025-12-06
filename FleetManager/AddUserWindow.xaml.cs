using System;
using System.Windows;
using MySql.Data.MySqlClient; // ✅ CORRECTION ICI (pas MySqlConnection)

namespace FleetManager
{
    public partial class AddUserWindow : Window
    {
        private string connectionString;

        public AddUserWindow(string connStr)
        {
            InitializeComponent();
            connectionString = connStr;
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            // Réinitialise le message d'erreur
            ErrorTextBlock.Text = "";

            // Récupère les valeurs
            string nom = NomTextBox.Text.Trim();
            string prenom = PrenomTextBox.Text.Trim();
            string email = EmailTextBox.Text.Trim();
            string password = PasswordBox.Password;
            string role = ((System.Windows.Controls.ComboBoxItem)RoleComboBox.SelectedItem).Content.ToString().ToLower();

            // Validation des champs
            if (string.IsNullOrEmpty(nom) || string.IsNullOrEmpty(prenom) ||
                string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ErrorTextBlock.Text = "⚠️ Tous les champs sont obligatoires !";
                return;
            }

            if (password.Length < 8)
            {
                ErrorTextBlock.Text = "⚠️ Le mot de passe doit contenir au moins 8 caractères !";
                return;
            }

            if (!email.Contains("@") || !email.Contains("."))
            {
                ErrorTextBlock.Text = "⚠️ Email invalide !";
                return;
            }

            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // Vérifie si l'email existe déjà
                    string checkQuery = "SELECT COUNT(*) FROM utilisateurs WHERE email = @Email";
                    using (var checkCmd = new MySqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@Email", email);
                        int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                        if (count > 0)
                        {
                            ErrorTextBlock.Text = "⚠️ Cet email est déjà utilisé !";
                            return;
                        }
                    }

                    // Hash du mot de passe avec BCrypt
                    string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

                    // Insère le nouvel utilisateur
                    string insertQuery = @"INSERT INTO utilisateurs 
                                          (nom, prenom, email, mot_de_passe, role, statut, date_creation) 
                                          VALUES (@Nom, @Prenom, @Email, @Password, @Role, 'actif', NOW())";

                    using (var cmd = new MySqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@Nom", nom);
                        cmd.Parameters.AddWithValue("@Prenom", prenom);
                        cmd.Parameters.AddWithValue("@Email", email);
                        cmd.Parameters.AddWithValue("@Password", hashedPassword);
                        cmd.Parameters.AddWithValue("@Role", role);

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show(
                    $"✅ Utilisateur créé avec succès !\n\n" +
                    $"Nom : {nom} {prenom}\n" +
                    $"Email : {email}\n" +
                    $"Rôle : {role}\n" +
                    $"Mot de passe : {password}",
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

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}