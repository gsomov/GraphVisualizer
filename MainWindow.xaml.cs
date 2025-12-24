using System;
using System.Windows;
using System.Windows.Controls;
using GraphVisualizer.Services;
using GraphVisualizer.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace GraphVisualizer
{
    public partial class MainWindow : Window
    {
        private GraphVisualizerService _visualizer;

        public MainWindow()
        {
            InitializeComponent();
            _visualizer = new GraphVisualizerService(GraphCanvas);

            // подключение кнопок 
            NewGraphButton.Click += NewGraphButton_Click;
            ClearGraphButton.Click += ClearGraphButton_Click;
            ClearPathButton.Click += ClearPathButton_Click;
            AddVertexButton.Click += AddVertexButton_Click;
            AddEdgeButton.Click += AddEdgeButton_Click;
            FindPathButton.Click += FindPathButton_Click;
            ShowMatrixButton.Click += ShowMatrixButton_Click;
            BuildFromMatrixButton.Click += BuildFromMatrixButton_Click;
            BuildFromListButton.Click += BuildFromListButton_Click;

            // примеры
            LoadExamples();

            UpdateGraphInfo();
            UpdateVertexComboBoxes();
        }

        private void LoadExamples()
        {
            Example1Text.Text = "0 1 0\n1 0 1\n0 1 0";
            Example2Text.Text = "0 2 6 0 0\n2 0 3 5 0\n6 3 0 1 0\n0 5 1 0 4\n0 0 0 4 0";
        }

        private void UpdateGraphInfo()
        {
            VertexCountText.Text = $"Вершин: {_visualizer.GetVertexCount()}";
            EdgeCountText.Text = $"Рёбер: {_visualizer.GetEdgeCount()}";
        }

        private void UpdateVertexComboBoxes()
        {
            StartVertexComboBox.Items.Clear();
            EndVertexComboBox.Items.Clear();

            var vertexNames = _visualizer.GetVertexNames();
            for (int i = 0; i < vertexNames.Count; i++)
            {
                StartVertexComboBox.Items.Add($"{i}: {vertexNames[i]}");
                EndVertexComboBox.Items.Add($"{i}: {vertexNames[i]}");
            }

            if (StartVertexComboBox.Items.Count > 0)
                StartVertexComboBox.SelectedIndex = 0;
            if (EndVertexComboBox.Items.Count > 0)
                EndVertexComboBox.SelectedIndex = Math.Min(1, EndVertexComboBox.Items.Count - 1);
        }

        private void UpdateMatrixViews()
        {
            AdjacencyMatrixText.Text = _visualizer.GetAdjacencyMatrixText();
            AdjacencyListText.Text = _visualizer.GetAdjacencyListText();
        }


        private void NewGraphButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Создать новый граф? Текущий граф будет удален.",
                "Новый граф", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _visualizer.ClearGraph();
                    UpdateMatrixViews();
                    UpdateGraphInfo();
                    UpdateVertexComboBoxes();
                    PathResultText.Text = "Путь будет отображен здесь";
                    StatusText.Text = "✓ Создан новый пустой граф";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ClearGraphButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Очистить граф? Все вершины и рёбра будут удалены.",
                "Очистить граф", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _visualizer.ClearGraph();
                    UpdateMatrixViews();
                    UpdateGraphInfo();
                    UpdateVertexComboBoxes();
                    PathResultText.Text = "Путь будет отображен здесь";
                    StatusText.Text = "✓ Граф очищен";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ClearPathButton_Click(object sender, RoutedEventArgs e)
        {
            _visualizer.ClearPath();
            PathResultText.Text = "Путь будет отображен здесь";
            StatusText.Text = "✓ Путь очищен";
        }

        private void AddVertexButton_Click(object sender, RoutedEventArgs e)
        {
            var inputDialog = new InputDialog("Введите имя вершины:", "Добавить вершину");
            if (inputDialog.ShowDialog() == true)
            {
                string vertexName = inputDialog.Answer;
                if (!string.IsNullOrWhiteSpace(vertexName))
                {
                    try
                    {
                        _visualizer.AddVertex(vertexName);
                        UpdateMatrixViews();
                        UpdateGraphInfo();
                        UpdateVertexComboBoxes();
                        StatusText.Text = $"✓ Добавлена вершина: {vertexName}";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void AddEdgeButton_Click(object sender, RoutedEventArgs e)
        {
            int vertexCount = _visualizer.GetVertexCount();
            if (vertexCount < 2)
            {
                MessageBox.Show("Добавьте хотя бы 2 вершины для создания ребра",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var vertexNames = _visualizer.GetVertexNames();
            var edgeDialog = new EdgeDialog(vertexNames);
            if (edgeDialog.ShowDialog() == true)
            {
                try
                {
                    _visualizer.AddEdge(
                        edgeDialog.FromVertex,
                        edgeDialog.ToVertex,
                        edgeDialog.Weight,
                        edgeDialog.IsDirected
                    );

                    UpdateMatrixViews();
                    UpdateGraphInfo();

                    string directionSymbol = edgeDialog.IsDirected ? "→" : "—";
                    string weightText = edgeDialog.Weight == 1.0 ? "" : $" (вес: {edgeDialog.Weight})";
                    StatusText.Text = $"✓ Добавлено ребро: {edgeDialog.FromVertex} {directionSymbol} {edgeDialog.ToVertex}{weightText}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void FindPathButton_Click(object sender, RoutedEventArgs e)
        {
            int vertexCount = _visualizer.GetVertexCount();
            if (vertexCount < 2)
            {
                MessageBox.Show("Добавьте хотя бы 2 вершины для поиска пути",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (StartVertexComboBox.SelectedItem == null || EndVertexComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите начальную и конечную вершины",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Извлекаем номера вершин из ComboBox
                string startItem = StartVertexComboBox.SelectedItem?.ToString();
                string endItem = EndVertexComboBox.SelectedItem?.ToString();

                if (string.IsNullOrEmpty(startItem) || string.IsNullOrEmpty(endItem))
                {
                    MessageBox.Show("Не удалось определить выбранные вершины",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string[] startParts = startItem.Split(':');
                string[] endParts = endItem.Split(':');

                if (startParts.Length < 1 || endParts.Length < 1)
                {
                    MessageBox.Show("Неверный формат выбранных вершин",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!int.TryParse(startParts[0].Trim(), out int startVertex) ||
                    !int.TryParse(endParts[0].Trim(), out int endVertex))
                {
                    MessageBox.Show("Не удалось распознать номера вершин",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Проверяем валидность индексов вершин
                if (startVertex < 0 || startVertex >= vertexCount ||
                    endVertex < 0 || endVertex >= vertexCount)
                {
                    MessageBox.Show($"Неверные номера вершин. Допустимый диапазон: 0-{vertexCount - 1}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (startVertex == endVertex)
                {
                    MessageBox.Show("Начальная и конечная вершины должны быть разными",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Ищем кратчайший путь
                var path = _visualizer.FindShortestPath(startVertex, endVertex);

                if (path == null || path.Count == 0)
                {
                    PathResultText.Text = $"Путь: Нет пути между {startVertex} и {endVertex}";
                    StatusText.Text = $"✗ Путь не найден: {startVertex} → {endVertex}";
                    _visualizer.ClearPath();
                }
                else
                {
                    // Форматируем путь для отображения
                    string pathString = string.Join(" → ", path);
                    PathResultText.Text = $"Путь: {pathString}";
                    StatusText.Text = $"✓ Найден кратчайший путь: {startVertex} → {endVertex}";

                    // Визуализируем путь
                    _visualizer.HighlightPath(path);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при поиске пути: {ex.Message}\n\nДетали: {ex.GetType().Name}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowMatrixButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateMatrixViews();
            StatusText.Text = "📋 Матрица и список смежности обновлены";
        }

        private void BuildFromMatrixButton_Click(object sender, RoutedEventArgs e)
        {
            var matrixDialog = new MatrixInputDialog();
            if (matrixDialog.ShowDialog() == true)
            {
                if (!InputValidators.ValidateMatrix(matrixDialog.MatrixText, out string error))
                {
                    MessageBox.Show($"Ошибка в формате матрицы:\n{error}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                try
                {
                    _visualizer.BuildFromMatrix(matrixDialog.MatrixText);
                    UpdateMatrixViews();
                    UpdateGraphInfo();
                    UpdateVertexComboBoxes();
                    PathResultText.Text = "Путь будет отображен здесь";
                    StatusText.Text = "✓ Граф построен из матрицы смежности";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при построении графа:\n{ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BuildFromListButton_Click(object sender, RoutedEventArgs e)
        {
            var listDialog = new ListInputDialog();
            if (listDialog.ShowDialog() == true)
            {
                if (!InputValidators.ValidateAdjacencyList(listDialog.ListText, out string error))
                {
                    MessageBox.Show($"Ошибка в формате списка:\n{error}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                try
                {
                    _visualizer.BuildFromAdjacencyList(listDialog.ListText);
                    UpdateMatrixViews();
                    UpdateGraphInfo();
                    UpdateVertexComboBoxes();
                    PathResultText.Text = "Путь будет отображен здесь";
                    StatusText.Text = "✓ Граф построен из списка смежности";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при построении графа:\n{ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
    // === Вспомогательные диалоговые окна ===

    public class InputDialog : Window
    {
        public string Answer { get; set; }

        public InputDialog(string question, string title)
        {
            Width = 300;
            Height = 150;
            Title = title;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var stackPanel = new StackPanel { Margin = new Thickness(10) };

            stackPanel.Children.Add(new TextBlock
            {
                Text = question,
                Margin = new Thickness(0, 0, 0, 10)
            });

            var textBox = new TextBox();
            stackPanel.Children.Add(textBox);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 10, 0, 0)
            };

            var okButton = new Button { Content = "OK", Width = 80, Margin = new Thickness(0, 0, 10, 0) };
            okButton.Click += (sender, e) =>
            {
                Answer = textBox.Text;
                DialogResult = true;
                Close();
            };

            var cancelButton = new Button { Content = "Отмена", Width = 80 };
            cancelButton.Click += (sender, e) =>
            {
                DialogResult = false;
                Close();
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            stackPanel.Children.Add(buttonPanel);

            Content = stackPanel;
        }
    }

    public class EdgeDialog : Window
    {
        public int FromVertex { get; set; }
        public int ToVertex { get; set; }
        public double Weight { get; set; }
        public bool IsDirected { get; set; }

        public EdgeDialog(List<string> vertexNames)
        {
            Width = 400;
            Height = 350;
            Title = "Добавить ребро";
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var stackPanel = new StackPanel { Margin = new Thickness(10) };

            // Выбор вершин
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // От вершины
            var fromLabel = new TextBlock { Text = "От вершины:", Margin = new Thickness(0, 0, 10, 5) };
            Grid.SetRow(fromLabel, 0);
            Grid.SetColumn(fromLabel, 0);
            grid.Children.Add(fromLabel);

            var fromComboBox = new ComboBox { Margin = new Thickness(0, 0, 10, 10) };
            for (int i = 0; i < vertexNames.Count; i++)
                fromComboBox.Items.Add($"{i}: {vertexNames[i]}");
            fromComboBox.SelectedIndex = 0;
            Grid.SetRow(fromComboBox, 1);
            Grid.SetColumn(fromComboBox, 0);
            grid.Children.Add(fromComboBox);

            // До вершины
            var toLabel = new TextBlock { Text = "До вершины:", Margin = new Thickness(0, 0, 0, 5) };
            Grid.SetRow(toLabel, 0);
            Grid.SetColumn(toLabel, 1);
            grid.Children.Add(toLabel);

            var toComboBox = new ComboBox { Margin = new Thickness(0, 0, 0, 10) };
            for (int i = 0; i < vertexNames.Count; i++)
                toComboBox.Items.Add($"{i}: {vertexNames[i]}");
            toComboBox.SelectedIndex = vertexNames.Count > 1 ? 1 : 0;
            Grid.SetRow(toComboBox, 1);
            Grid.SetColumn(toComboBox, 1);
            grid.Children.Add(toComboBox);

            stackPanel.Children.Add(grid);

            // Вес ребра
            var weightPanel = new StackPanel { Margin = new Thickness(0, 10, 0, 0) };
            weightPanel.Children.Add(new TextBlock { Text = "Вес ребра (оставьте пустым или 0 для веса 1.0):" });

            var weightTextBox = new TextBox { Text = "" };
            weightTextBox.ToolTip = "Пусто или 0 = вес 1.0\nПримеры: 2.5, 3, 0.7";
            weightPanel.Children.Add(weightTextBox);

            stackPanel.Children.Add(weightPanel);

            // Ориентированное ребро
            var directedCheckBox = new CheckBox
            {
                Content = "Ориентированное ребро (со стрелкой)",
                Margin = new Thickness(0, 15, 0, 0),
                FontWeight = FontWeights.Bold
            };
            stackPanel.Children.Add(directedCheckBox);

            // Кнопки
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 20, 0, 0)
            };

            var okButton = new Button
            {
                Content = "✅ Добавить",
                Width = 120,
                Margin = new Thickness(0, 0, 10, 0),
                Background = Brushes.LightGreen
            };
            okButton.Click += (sender, e) =>
            {
                FromVertex = int.Parse(fromComboBox.SelectedItem.ToString().Split(':')[0]);
                ToVertex = int.Parse(toComboBox.SelectedItem.ToString().Split(':')[0]);

                string weightText = weightTextBox.Text.Trim();
                if (string.IsNullOrEmpty(weightText) || weightText == "0")
                {
                    Weight = 1.0;
                }
                else
                {
                    weightText = weightText.Replace(',', '.');
                    if (double.TryParse(weightText, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out double weight))
                    {
                        Weight = weight;
                    }
                    else
                    {
                        MessageBox.Show("Неверный формат веса. Используется вес 1.0",
                            "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                        Weight = 1.0;
                    }
                }

                IsDirected = directedCheckBox.IsChecked ?? false;
                DialogResult = true;
                Close();
            };

            var cancelButton = new Button
            {
                Content = "❌ Отмена",
                Width = 120,
                Background = Brushes.LightCoral
            };
            cancelButton.Click += (sender, e) =>
            {
                DialogResult = false;
                Close();
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            stackPanel.Children.Add(buttonPanel);

            Content = stackPanel;
        }
    }

    public class MatrixInputDialog : Window
    {
        public string MatrixText { get; set; }

        public MatrixInputDialog()
        {
            Width = 500;
            Height = 400;
            Title = "Ввод матрицы смежности";
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var textBox = new TextBox
            {
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 12,
                Margin = new Thickness(10)
            };

            textBox.Text = "0 1 0\n1 0 1\n0 1 0";

            textBox.TextChanged += (sender, e) =>
            {
                if (InputValidators.ValidateMatrix(textBox.Text, out string error))
                {
                    textBox.Background = Brushes.White;
                    textBox.ToolTip = "Корректный формат матрицы";
                }
                else
                {
                    textBox.Background = Brushes.LightYellow;
                    textBox.ToolTip = error;
                }
            };

            Grid.SetRow(textBox, 0);
            grid.Children.Add(textBox);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(10)
            };

            var okButton = new Button { Content = "Построить", Width = 100, Margin = new Thickness(0, 0, 10, 0) };
            okButton.Click += (sender, e) =>
            {
                MatrixText = textBox.Text;
                DialogResult = true;
                Close();
            };

            var cancelButton = new Button { Content = "Отмена", Width = 100 };
            cancelButton.Click += (sender, e) =>
            {
                DialogResult = false;
                Close();
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            Grid.SetRow(buttonPanel, 1);
            grid.Children.Add(buttonPanel);

            Content = grid;
        }
    }

    public class ListInputDialog : Window
    {
        public string ListText { get; set; }

        public ListInputDialog()
        {
            Width = 500;
            Height = 400;
            Title = "Ввод списка смежности";
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var textBox = new TextBox
            {
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 12,
                Margin = new Thickness(10)
            };

            textBox.Text = "0: →1 →2\n1: →0 →3\n2: →0 →4\n3: →1\n4: →2";

            textBox.TextChanged += (sender, e) =>
            {
                if (InputValidators.ValidateAdjacencyList(textBox.Text, out string error))
                {
                    textBox.Background = Brushes.White;
                    textBox.ToolTip = "Корректный формат списка смежности";
                }
                else
                {
                    textBox.Background = Brushes.LightYellow;
                    textBox.ToolTip = error;
                }
            };

            Grid.SetRow(textBox, 0);
            grid.Children.Add(textBox);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(10)
            };

            var okButton = new Button { Content = "Построить", Width = 100, Margin = new Thickness(0, 0, 10, 0) };
            okButton.Click += (sender, e) =>
            {
                ListText = textBox.Text;
                DialogResult = true;
                Close();
            };

            var cancelButton = new Button { Content = "Отмена", Width = 100 };
            cancelButton.Click += (sender, e) =>
            {
                DialogResult = false;
                Close();
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            Grid.SetRow(buttonPanel, 1);
            grid.Children.Add(buttonPanel);

            Content = grid;
        }
    }
}