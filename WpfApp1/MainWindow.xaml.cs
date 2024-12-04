using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;
using System.Collections.Generic;

namespace ThreadAppWPF //varianta asta inca nu e ok, merge in ordine, dar nu anuleaza. 
    //doua array uri indexate, i pozitia
{
    public partial class MainWindow : Window
    {
        private CancellationTokenSource cancellationTokenSource;
        private string originalText;
        private bool isCanceled = false;

        private string[] criptateText; // Array pentru textul criptat
        private string[] decriptateText; // Array pentru textul decriptat

        public MainWindow()
        {
            InitializeComponent();
        }

        // Criptare folosind Parallel.ForEach
        private void btnExecutaTask_Click(object sender, RoutedEventArgs e)
        {
            originalText = txtOriginal.Text;

            if (string.IsNullOrWhiteSpace(originalText))
            {
                MessageBox.Show("Introduceți un text pentru criptare.");
                return;
            }

            cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = cancellationTokenSource.Token;

            btnCancel.IsEnabled = true;
            btnExecutaTask.IsEnabled = false;

            // Împărțim textul în subsiruri pentru paralelizare
            List<string> subsiruri = ImparteText(originalText);

            criptateText = new string[subsiruri.Count];  // Inițializăm array-ul pentru criptare
            decriptateText = new string[subsiruri.Count]; // Inițializăm array-ul pentru decriptare

            // Folosim un Task separat pentru criptare
            Task.Run(() =>
            {
                try
                {
                    // Parcurgem fiecare subsir din listă
                    Parallel.For(0, subsiruri.Count, i =>
                    {
                        if (token.IsCancellationRequested)
                        {
                            isCanceled = true;
                            return;
                        }

                        // Criptare
                        string criptat = Criptare(subsiruri[i]);
                        criptateText[i] = criptat;  // Stocăm criptarea în array

                        // Afișăm textul criptat în caseta corespunzătoare
                        Dispatcher.Invoke(() =>
                        {
                            txtCriptat.Text = string.Join("", criptateText);
                        });

                        Debug.Print($"In thread-ul de criptare: {Thread.CurrentThread.ManagedThreadId}");

                        // Delay pentru a permite anularea
                        Thread.Sleep(5000);
                    });

                    if (!isCanceled)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show("Criptare completă.");
                        });
                    }
                }
                catch (OperationCanceledException)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("Criptare anulată de utilizator.");
                    });
                }
                finally
                {
                    Dispatcher.Invoke(() =>
                    {
                        btnCancel.IsEnabled = false;
                        btnExecutaTask.IsEnabled = true;
                    });
                }
            });
        }

        // Anularea criptării
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
            }
        }

        // Decriptare
        private void btnDecripteaza_Click(object sender, RoutedEventArgs e)
        {
            if (criptateText == null || criptateText.Length == 0)
            {
                MessageBox.Show("Nu există text criptat de decriptat.");
                return;
            }

            // Folosim un Task separat pentru decriptare
            Task.Run(() =>
            {
                Parallel.For(0, criptateText.Length, i =>
                {
                    // Decriptare
                    string decriptat = Decriptare(criptateText[i]);
                    decriptateText[i] = decriptat;  // Stocăm decriptarea în array

                    Dispatcher.Invoke(() =>
                    {
                        // Afișăm decriptarea în ordinea corectă
                        txtDecriptat.Text = string.Join("", decriptateText);
                    });

                    Debug.Print($"In thread-ul de decriptare: {Thread.CurrentThread.ManagedThreadId}");
                });

                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("Decriptare finalizată.");
                });
            });
        }

        // Criptare folosind XOR
        private string Criptare(string text)
        {
            Random rand = new Random();
            string rezultat = "";

            foreach (char c in text)
            {
                char randomChar = (char)rand.Next(128);
                rezultat += (char)(c ^ randomChar);
                rezultat += randomChar;
            }

            return rezultat;
        }

        // Decriptare
        private string Decriptare(string textCriptat)
        {
            string rezultat = "";

            for (int i = 0; i < textCriptat.Length; i += 2)
            {
                if (i + 1 >= textCriptat.Length)
                {
                    break;
                }

                char criptatChar = textCriptat[i];
                char randomChar = textCriptat[i + 1];
                rezultat += (char)(criptatChar ^ randomChar);
            }

            return rezultat;
        }

        // Împărțim textul în subsiruri pentru paralelizare
        private List<string> ImparteText(string text)
        {
            int nrThreads = Environment.ProcessorCount;  // Asigurăm că nrThreads este un int
            int lungimeSubsir = (int)Math.Ceiling((double)text.Length / nrThreads);  // Conversia în int
            List<string> subsiruri = new List<string>();

            for (int i = 0; i < text.Length; i += lungimeSubsir)
            {
                if (i + lungimeSubsir > text.Length)
                    subsiruri.Add(text.Substring(i));
                else
                    subsiruri.Add(text.Substring(i, lungimeSubsir));
            }

            return subsiruri;
        }
    }
}





/*using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;
using System.Collections.Generic;

//varianta lab 9 exercitiu 2
//array - doua- in loc de subsiruri. i va fi pozitia si va ordona textul la decriptare
namespace ThreadAppWPF
{
    public partial class MainWindow : Window
    {
        private CancellationTokenSource cancellationTokenSource;
        private ConcurrentQueue<string> criptareQueue = new ConcurrentQueue<string>();
        private string originalText;
        private bool isCanceled = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        // Criptare folosind Parallel.ForEach
        private void btnExecutaTask_Click(object sender, RoutedEventArgs e)
        {
            originalText = txtOriginal.Text;

            if (string.IsNullOrWhiteSpace(originalText))
            {
                MessageBox.Show("Introduceți un text pentru criptare.");
                return;
            }

            cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = cancellationTokenSource.Token;

            btnCancel.IsEnabled = true;
            btnExecutaTask.IsEnabled = false;

            List<string> subsiruri = ImparteText(originalText);

            // Folosim un Task separat pentru criptare
            Task.Run(() =>
            {
                try
                {
                    foreach (var subText in subsiruri)
                    {
                        // Verificăm continuu dacă anularea a fost solicitată
                        if (token.IsCancellationRequested)
                        {
                            isCanceled = true;
                            break;
                        }

                        // Criptare
                        string criptat = Criptare(subText);
                        criptareQueue.Enqueue(criptat);

                        // Afișăm textul criptat în caseta corespunzătoare
                        Dispatcher.Invoke(() =>
                        {
                            txtCriptat.Text = string.Join("", criptareQueue);
                        });

                        Debug.Print($"In thread-ul de criptare: {Thread.CurrentThread.ManagedThreadId}");

                        // Delay pentru a permite anularea
                        Thread.Sleep(1000);
                    }

                    if (!isCanceled)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show("Criptare completă.");
                        });
                    }
                }
                catch (OperationCanceledException)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("Criptare anulată de utilizator.");
                    });
                }
                finally
                {
                    Dispatcher.Invoke(() =>
                    {
                        btnCancel.IsEnabled = false;
                        btnExecutaTask.IsEnabled = true;
                    });
                }
            });
        }

        // Anularea criptării
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
            }
        }

        // Decriptare
        private void btnDecripteaza_Click(object sender, RoutedEventArgs e)
        {
            if (criptareQueue.IsEmpty)
            {
                MessageBox.Show("Nu există text criptat de decriptat.");
                return;
            }

            // Preluăm doar textul criptat până la anulare
            var criptatePartial = criptareQueue.ToList();
            ConcurrentQueue<string> decriptareQueue = new ConcurrentQueue<string>();

            // Folosim Parallel.ForEach pentru paralelizarea decriptării
            Task.Run(() =>
            {
                Parallel.ForEach(criptatePartial, subText =>
                {
                    // Decriptare
                    string decriptat = Decriptare(subText);
                    decriptareQueue.Enqueue(decriptat);

                    Dispatcher.Invoke(() =>
                    {
                        txtDecriptat.Text = string.Join("", decriptareQueue);
                    });

                    Debug.Print($"In thread-ul de decriptare: {Thread.CurrentThread.ManagedThreadId}");
                });

                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("Decriptare finalizată.");
                });
            });
        }

        // Criptare folosind XOR
        private string Criptare(string text)
        {
            Random rand = new Random();
            string rezultat = "";

            foreach (char c in text)
            {
                char randomChar = (char)rand.Next(128);
                rezultat += (char)(c ^ randomChar);
                rezultat += randomChar;
            }

            return rezultat;
        }

        // Decriptare
        private string Decriptare(string textCriptat)
        {
            string rezultat = "";

            for (int i = 0; i < textCriptat.Length; i += 2)
            {
                if (i + 1 >= textCriptat.Length)
                {
                    break;
                }

                char criptatChar = textCriptat[i];
                char randomChar = textCriptat[i + 1];
                rezultat += (char)(criptatChar ^ randomChar);
            }

            return rezultat;
        }

        // Împărțim textul în subsiruri pentru paralelizare
        private List<string> ImparteText(string text)
        {
            int nrThreads = Environment.ProcessorCount;
            int lungimeSubsir = (int)Math.Ceiling((double)text.Length / nrThreads);
            List<string> subsiruri = new List<string>();

            for (int i = 0; i < text.Length; i += lungimeSubsir)
            {
                if (i + lungimeSubsir > text.Length)
                    subsiruri.Add(text.Substring(i));
                else
                    subsiruri.Add(text.Substring(i, lungimeSubsir));
            }

            return subsiruri;
        }
    }
}
*/


/*using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ThreadAppWPF
{//la varianta aceasta, lab9ex2, nu pot apasa butonul de anulare
    public partial class MainWindow : Window
    {
        private CancellationTokenSource cancellationTokenSource;
        private string textCriptat = "";

        public MainWindow()
        {
            InitializeComponent();
        }

        // Criptare folosind Parallel.For
        private void btnExecutaTask_Click(object sender, RoutedEventArgs e)
        {
            string textOriginal = txtOriginal.Text;
            if (string.IsNullOrWhiteSpace(textOriginal))
            {
                MessageBox.Show("Introduceți un text pentru criptare.");
                return;
            }

            cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = cancellationTokenSource.Token;
            btnCancel.IsEnabled = true; // Activăm butonul Cancel
            btnExecutaTask.IsEnabled = false; // Dezactivăm butonul de criptare

            List<string> subsiruri = ImparteText(textOriginal);
            ConcurrentQueue<string> rezultateCriptate = new ConcurrentQueue<string>();

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            Parallel.For(0, subsiruri.Count, i =>
            {
                token.ThrowIfCancellationRequested();

                // Simulăm un proces mai lung
                Thread.Sleep(5000);

                // Criptăm subsirul curent
                string rezultat = Criptare(subsiruri[i]);
                rezultateCriptate.Enqueue(rezultat);

                // Afișăm thread-ul folosit
                Debug.Print($"In thread-ul de criptare: {Thread.CurrentThread.ManagedThreadId}");
            });

            stopwatch.Stop();

            // Finalizăm textul criptat
            Dispatcher.Invoke(() =>
            {
                textCriptat = string.Join("", rezultateCriptate);
                txtCriptat.Text = textCriptat;
                lblTimpExecutie.Content = $"Timp de execuție: {stopwatch.ElapsedMilliseconds} ms";
            });

            btnCancel.IsEnabled = false;
            btnExecutaTask.IsEnabled = true;
        }

        // Buton pentru anulare
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                btnCancel.IsEnabled = false; // Dezactivăm butonul Cancel
            }
        }

        // Buton pentru decriptare
        private void btnDecripteaza_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textCriptat))
            {
                MessageBox.Show("Nu există text de decriptat.");
                return;
            }

            Task.Run(() =>
            {
                string textDecriptat = Decriptare(textCriptat);
                Dispatcher.Invoke(() =>
                {
                    txtDecriptat.Text = textDecriptat;
                });
            });
        }

        // Funcția de criptare (XOR)
        private string Criptare(string text)
        {
            Random rand = new Random();
            string rezultat = "";
            foreach (char c in text)
            {
                char randomChar = (char)rand.Next(128);
                rezultat += (char)(c ^ randomChar);
                rezultat += randomChar;
            }
            return rezultat;
        }

        // Funcția de decriptare
        private string Decriptare(string textCriptat)
        {
            string rezultat = "";
            for (int i = 0; i < textCriptat.Length; i += 2)
            {
                // Ignorăm "CANCEL", care are lungimea mai mare de 2 caractere
                if (i + 1 >= textCriptat.Length || textCriptat[i] == 'C')
                {
                    rezultat += "CANCEL";
                    break;
                }

                char criptatChar = textCriptat[i];
                char randomChar = textCriptat[i + 1];
                rezultat += (char)(criptatChar ^ randomChar);
            }
            return rezultat;
        }

        // Împărțim textul în subsiruri în funcție de numărul de procesoare
        private List<string> ImparteText(string text)
        {
            int nrThreads = Environment.ProcessorCount;
            int lungimeSubsir = (int)Math.Ceiling((double)text.Length / nrThreads);
            List<string> subsiruri = new List<string>();
            for (int i = 0; i < text.Length; i += lungimeSubsir)
            {
                if (i + lungimeSubsir > text.Length)
                    subsiruri.Add(text.Substring(i));
                else
                    subsiruri.Add(text.Substring(i, lungimeSubsir));
            }
            return subsiruri;
        }
    }
}*/






/*using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

//varianta lab 8

namespace ThreadAppWPF
{
    public partial class MainWindow : Window
    {
        private CancellationTokenSource cancellationTokenSource;
        private string textCriptat = "";

        public MainWindow()
        {
            InitializeComponent();
        }

        // Criptare folosind Task-uri
        private void btnExecutaTask_Click(object sender, RoutedEventArgs e)
        {
            string textOriginal = txtOriginal.Text;
            if (string.IsNullOrWhiteSpace(textOriginal))
            {
                MessageBox.Show("Introduceți un text pentru criptare.");
                return;
            }

            cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = cancellationTokenSource.Token;
            btnCancel.IsEnabled = true; // Activăm butonul Cancel
            btnExecutaTask.IsEnabled = false; // Dezactivăm butonul de criptare

            List<string> subsiruri = ImparteText(textOriginal);
            string[] rezultateCriptate = new string[subsiruri.Count];

            Task.Run(async () =>
            {
                try
                {
                    for (int i = 0; i < subsiruri.Count; i++)
                    {
                        token.ThrowIfCancellationRequested();

                        // Simulăm un proces mai lung
                        await Task.Delay(500);

                        // Criptăm subsirul curent
                        rezultateCriptate[i] = Criptare(subsiruri[i]);

                        // Actualizăm textul criptat parțial
                        Dispatcher.Invoke(() =>
                        {
                            txtCriptat.Text = string.Join("", rezultateCriptate);
                        });
                    }

                    // Finalizăm textul criptat
                    Dispatcher.Invoke(() =>
                    {
                        textCriptat = string.Join("", rezultateCriptate);
                        txtCriptat.Text = textCriptat;
                    });
                }
                catch (OperationCanceledException)
                {
                    // Afișăm textul criptat până la momentul anulării și adăugăm "CANCEL" pentru restul
                    Dispatcher.Invoke(() =>
                    {
                        textCriptat = string.Join("", rezultateCriptate);
                        int caractereCriptate = textCriptat.Length;
                        int caractereRamase = textOriginal.Length - caractereCriptate;

                        // Adăugăm "CANCEL" pentru textul necriptat
                        for (int i = 0; i < caractereRamase; i++)
                        {
                            textCriptat += "CANCEL";
                        }

                        txtCriptat.Text = textCriptat;
                    });
                }
                finally
                {
                    Dispatcher.Invoke(() =>
                    {
                        btnCancel.IsEnabled = false;
                        btnExecutaTask.IsEnabled = true;
                    });
                }
            });
        }

        // Buton pentru anulare
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                btnCancel.IsEnabled = false; // Dezactivăm butonul Cancel
            }
        }

        // Buton pentru decriptare
        private void btnDecripteaza_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textCriptat))
            {
                MessageBox.Show("Nu există text de decriptat.");
                return;
            }

            Task.Run(() =>
            {
                string textDecriptat = Decriptare(textCriptat);
                Dispatcher.Invoke(() =>
                {
                    txtDecriptat.Text = textDecriptat;
                });
            });
        }

        // Funcția de criptare (XOR)
        private string Criptare(string text)
        {
            Random rand = new Random();
            string rezultat = "";
            foreach (char c in text)
            {
                char randomChar = (char)rand.Next(128);
                rezultat += (char)(c ^ randomChar);
                rezultat += randomChar;
            }
            return rezultat;
        }

        // Funcția de decriptare
        private string Decriptare(string textCriptat)
        {
            string rezultat = "";
            for (int i = 0; i < textCriptat.Length; i += 2)
            {
                // Ignorăm "CANCEL", care are lungimea mai mare de 2 caractere
                if (i + 1 >= textCriptat.Length || textCriptat[i] == 'C')
                {
                    rezultat += "CANCEL";
                    break;
                }

                char criptatChar = textCriptat[i];
                char randomChar = textCriptat[i + 1];
                rezultat += (char)(criptatChar ^ randomChar);
            }
            return rezultat;
        }

        // Împărțim textul în subsiruri în funcție de numărul de procesoare
        private List<string> ImparteText(string text)
        {
            int nrThreads = Environment.ProcessorCount;
            int lungimeSubsir = (int)Math.Ceiling((double)text.Length / nrThreads);
            List<string> subsiruri = new List<string>();
            for (int i = 0; i < text.Length; i += lungimeSubsir)
            {
                if (i + lungimeSubsir > text.Length)
                    subsiruri.Add(text.Substring(i));
                else
                    subsiruri.Add(text.Substring(i, lungimeSubsir));
            }
            return subsiruri;
        }
    }
}

*/


/*using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ThreadAppWPF
{
    public partial class MainWindow : Window
    {
        private CancellationTokenSource cancellationTokenSource;
        private string textCriptat = "";

        public MainWindow()
        {
            InitializeComponent();
        }

        // Criptare folosind Task-uri
        private void btnExecutaTask_Click(object sender, RoutedEventArgs e)
        {
            string textOriginal = txtOriginal.Text;
            if (string.IsNullOrWhiteSpace(textOriginal))
            {
                MessageBox.Show("Introduceți un text pentru criptare.");
                return;
            }

            cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = cancellationTokenSource.Token;
            btnCancel.IsEnabled = true; // Activăm butonul Cancel
            btnExecutaTask.IsEnabled = false; // Dezactivăm butonul de criptare

            List<string> subsiruri = ImparteText(textOriginal);
            string[] rezultateCriptate = new string[subsiruri.Count];

            Task.Run(async () =>
            {
                try
                {
                    for (int i = 0; i < subsiruri.Count; i++)
                    {
                        token.ThrowIfCancellationRequested();

                        // Simulăm un proces mai lung
                        await Task.Delay(500);

                        // Criptăm subsirul curent
                        rezultateCriptate[i] = Criptare(subsiruri[i]);

                        // Actualizăm textul criptat parțial
                        Dispatcher.Invoke(() =>
                        {
                            txtCriptat.Text = string.Join("", rezultateCriptate);
                        });
                    }

                    // Finalizăm textul criptat
                    Dispatcher.Invoke(() =>
                    {
                        textCriptat = string.Join("", rezultateCriptate);
                        txtCriptat.Text = textCriptat;
                    });
                }
                catch (OperationCanceledException)
                {
                    // Afișăm textul criptat până la momentul anulării
                    Dispatcher.Invoke(() =>
                    {
                        textCriptat = string.Join("", rezultateCriptate);
                        txtCriptat.Text = textCriptat;

                        // Textul rămas necriptat
                        int startIndex = textCriptat.Length;
                        if (startIndex < textOriginal.Length)
                        {
                            txtOriginal.Text = "CANCEL";
                        }
                    });
                }
                finally
                {
                    Dispatcher.Invoke(() =>
                    {
                        btnCancel.IsEnabled = false;
                        btnExecutaTask.IsEnabled = true;
                    });
                }
            });
        }

        // Buton pentru anulare
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                btnCancel.IsEnabled = false; // Dezactivăm butonul Cancel
            }
        }

        // Buton pentru decriptare
        private void btnDecripteaza_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textCriptat))
            {
                MessageBox.Show("Nu există text de decriptat.");
                return;
            }

            Task.Run(() =>
            {
                string textDecriptat = Decriptare(textCriptat);
                Dispatcher.Invoke(() =>
                {
                    txtDecriptat.Text = textDecriptat;
                });
            });
        }

        // Funcția de criptare (XOR)
        private string Criptare(string text)
        {
            Random rand = new Random();
            string rezultat = "";
            foreach (char c in text)
            {
                char randomChar = (char)rand.Next(128);
                rezultat += (char)(c ^ randomChar);
                rezultat += randomChar;
            }
            return rezultat;
        }

        // Funcția de decriptare
        private string Decriptare(string textCriptat)
        {
            string rezultat = "";
            for (int i = 0; i < textCriptat.Length; i += 2)
            {
                char criptatChar = textCriptat[i];
                char randomChar = textCriptat[i + 1];
                rezultat += (char)(criptatChar ^ randomChar);
            }
            return rezultat;
        }

        // Împărțim textul în subsiruri în funcție de numărul de procesoare
        private List<string> ImparteText(string text)
        {
            int nrThreads = Environment.ProcessorCount;
            int lungimeSubsir = (int)Math.Ceiling((double)text.Length / nrThreads);
            List<string> subsiruri = new List<string>();
            for (int i = 0; i < text.Length; i += lungimeSubsir)
            {
                if (i + lungimeSubsir > text.Length)
                    subsiruri.Add(text.Substring(i));
                else
                    subsiruri.Add(text.Substring(i, lungimeSubsir));
            }
            return subsiruri;
        }
    }
}
*/