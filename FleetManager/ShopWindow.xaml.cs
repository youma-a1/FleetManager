using System.Windows;
using MySql.Data.MySqlClient;
using System.Data;

namespace FleetManager
{
    public partial class ShopWindow : Window
    {
        private string connectionString;

        public ShopWindow(string connStr)
        {
            InitializeComponent();
            connectionString = connStr;

            // Met à jour le titre avec le nom de l'utilisateur
            this.Title = $"Boutique - {CurrentUser.Prenom} {CurrentUser.Nom} ({CurrentUser.Role})";

            LoadVehicles();
        }

        private void LoadVehicles()
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT id, marque, modele, immatriculation, carburant, kilometrage, statut FROM vehicules";

                    using (var adapter = new MySqlDataAdapter(query, conn))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        VehiclesDataGrid.ItemsSource = dt.DefaultView;
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Erreur base de données : " + ex.Message, "Erreur");
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            // Retour à la fenêtre login
            MainWindow login = new MainWindow();
            login.Show();
            this.Close();
        }
    }
}