using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using GraphVisualizer.Models;
using System.Collections.Generic;
using System.Linq;

namespace GraphVisualizer.Services
{
    public class GraphVisualizerService
    {
        private Canvas _canvas;
        private Graph _graph;

        private Dictionary<int, Ellipse> _vertexShapes = new Dictionary<int, Ellipse>();
        private Dictionary<string, Line> _edgeShapes = new Dictionary<string, Line>();
        private Dictionary<int, TextBlock> _vertexLabels = new Dictionary<int, TextBlock>();

        private SolidColorBrush _vertexFill = Brushes.LightBlue;
        private SolidColorBrush _vertexBorder = Brushes.DarkBlue;
        private SolidColorBrush _edgeBrush = Brushes.Black;
        private SolidColorBrush _directedEdgeBrush = Brushes.DarkRed;
        private SolidColorBrush _pathBrush = Brushes.Green;
        private SolidColorBrush _pathVertexBrush = Brushes.Gold;

        private List<int> _currentPath = new List<int>();

        public GraphVisualizerService(Canvas canvas)
        {
            _canvas = canvas;
            _graph = new Graph();
        }

        public void DrawGraph()
        {
            try
            {
                // Проверяем, что canvas инициализирован
                if (_canvas == null)
                    return;

                _canvas.Children.Clear();
                _vertexShapes.Clear();
                _edgeShapes.Clear();
                _vertexLabels.Clear();

                // Проверяем, что граф инициализирован
                if (_graph == null || _graph.Vertices == null || _graph.Edges == null)
                    return;

                // Для неориентированных графов рисуем только одно ребро (From < To),
                // чтобы избежать дублирования весов на экране
                var drawnEdges = new HashSet<string>();
                
                foreach (var edge in _graph.Edges)
                {
                    // Для неориентированных рёбер рисуем только одно направление
                    if (!edge.IsDirected)
                    {
                        string edgeKey = edge.From < edge.To ? $"{edge.From}-{edge.To}" : $"{edge.To}-{edge.From}";
                        if (drawnEdges.Contains(edgeKey))
                            continue;
                        drawnEdges.Add(edgeKey);
                    }
                    else
                    {
                        // Для ориентированных рёбер проверяем, есть ли обратное ребро с тем же весом
                        // Если есть - обрабатываем как неориентированное (одна линия, без стрелок, один вес)
                        var reverseEdge = _graph.Edges.FirstOrDefault(e => 
                            e.From == edge.To && e.To == edge.From && 
                            e.IsDirected && Math.Abs(e.Weight - edge.Weight) < 0.0001);
                        
                        if (reverseEdge != null)
                        {
                            // Это двустороннее ребро с одинаковым весом - рисуем как неориентированное
                            string edgeKey = edge.From < edge.To ? $"{edge.From}-{edge.To}" : $"{edge.To}-{edge.From}";
                            if (drawnEdges.Contains(edgeKey))
                                continue;
                            drawnEdges.Add(edgeKey);
                            
                            // Создаём временное неориентированное ребро для отрисовки
                            var undirectedEdge = new Edge(edge.From, edge.To, edge.Weight, false);
                            DrawEdge(undirectedEdge);
                            continue;
                        }
                        else
                        {
                            // Обычное ориентированное ребро
                            string edgeKey = $"{edge.From}-{edge.To}";
                            if (drawnEdges.Contains(edgeKey))
                                continue;
                            drawnEdges.Add(edgeKey);
                        }
                    }
                    
                    DrawEdge(edge);
                }

                foreach (var vertex in _graph.Vertices)
                {
                    DrawVertex(vertex);
                }
            }
            catch (Exception ex)
            {
                // Логируем ошибку, но не падаем
                System.Diagnostics.Debug.WriteLine($"Ошибка в DrawGraph: {ex.Message}");
            }
        }

        private void DrawVertex(Vertex vertex)
        {
            try
            {
                if (_canvas == null || vertex == null)
                    return;

                // Определяем цвет вершины (обычный или часть пути)
                Brush fillBrush = _currentPath.Contains(vertex.Id) ? _pathVertexBrush : _vertexFill;

                var ellipse = new Ellipse
                {
                    Width = 40,
                    Height = 40,
                    Fill = fillBrush,
                    Stroke = _vertexBorder,
                    StrokeThickness = 2
                };

                Canvas.SetLeft(ellipse, vertex.X - 20);
                Canvas.SetTop(ellipse, vertex.Y - 20);

                var label = new TextBlock
                {
                    Text = vertex.Name,
                    Foreground = Brushes.Black,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Width = 30,
                    TextAlignment = TextAlignment.Center
                };

                Canvas.SetLeft(label, vertex.X - 15);
                Canvas.SetTop(label, vertex.Y - 10);

                _canvas.Children.Add(ellipse);
                _canvas.Children.Add(label);

                _vertexShapes[vertex.Id] = ellipse;
                _vertexLabels[vertex.Id] = label;

                MakeDraggable(ellipse, vertex);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в DrawVertex: {ex.Message}");
            }
        }

        private void DrawEdge(Edge edge)
        {
            try
            {
                if (_canvas == null || edge == null || _graph == null)
                    return;

                var fromVertex = _graph.GetVertex(edge.From);
                var toVertex = _graph.GetVertex(edge.To);

                if (fromVertex == null || toVertex == null) return;

                // Проверяем, является ли ребро частью пути
                bool isInPath = IsEdgeInPath(edge.From, edge.To);
                Brush edgeColor = isInPath ? _pathBrush : (edge.IsDirected ? _directedEdgeBrush : _edgeBrush);
                double thickness = isInPath ? 4 : 2;

                // Радиус вершины (круга) - 20 пикселей
                double vertexRadius = 20;
                
                // Вычисляем точки на границах кругов
                double angle = Math.Atan2(toVertex.Y - fromVertex.Y, toVertex.X - fromVertex.X);
                double fromX = fromVertex.X + vertexRadius * Math.Cos(angle);
                double fromY = fromVertex.Y + vertexRadius * Math.Sin(angle);
                double toX = toVertex.X - vertexRadius * Math.Cos(angle);
                double toY = toVertex.Y - vertexRadius * Math.Sin(angle);

                var line = new Line
                {
                    X1 = fromX,
                    Y1 = fromY,
                    X2 = toX,
                    Y2 = toY,
                    Stroke = edgeColor,
                    StrokeThickness = thickness,
                    Tag = "edge"
                };

                _canvas.Children.Add(line);

                // Определяем, является ли граф ориентированным
                bool isGraphDirected = _graph.IsDirected();

                // Рисуем стрелки только для ориентированного графа и только для ориентированных рёбер
                if (isGraphDirected && edge.IsDirected && !isInPath) // Для пути не рисуем стрелки поверх толстых линий
                {
                    double arrowLength = 10;
                    double arrowAngle = 25 * Math.PI / 180;
                    
                    // Вычисляем точку на границе круга (не в центре)
                    // Используем уже вычисленный angle и vertexRadius
                    double arrowBaseX = toVertex.X - vertexRadius * Math.Cos(angle);
                    double arrowBaseY = toVertex.Y - vertexRadius * Math.Sin(angle);

                    var arrowLeft = new Line
                    {
                        X1 = arrowBaseX,
                        Y1 = arrowBaseY,
                        X2 = arrowBaseX - arrowLength * Math.Cos(angle - arrowAngle),
                        Y2 = arrowBaseY - arrowLength * Math.Sin(angle - arrowAngle),
                        Stroke = _directedEdgeBrush,
                        StrokeThickness = 2,
                        Tag = "arrow"
                    };

                    var arrowRight = new Line
                    {
                        X1 = arrowBaseX,
                        Y1 = arrowBaseY,
                        X2 = arrowBaseX - arrowLength * Math.Cos(angle + arrowAngle),
                        Y2 = arrowBaseY - arrowLength * Math.Sin(angle + arrowAngle),
                        Stroke = _directedEdgeBrush,
                        StrokeThickness = 2,
                        Tag = "arrow"
                    };

                    _canvas.Children.Add(arrowLeft);
                    _canvas.Children.Add(arrowRight);
                }

                // Показываем все веса (включая 1.0) для всех графов
                var label = new TextBlock
                {
                    Text = FormatWeight(edge.Weight),
                    Foreground = isInPath ? Brushes.DarkGreen : Brushes.DarkGreen,
                    FontWeight = FontWeights.Bold,
                    Background = isInPath ? Brushes.LightYellow : Brushes.White,
                    Padding = new Thickness(2),
                    Tag = "edge-label"
                };

                double labelAngle = Math.Atan2(toVertex.Y - fromVertex.Y, toVertex.X - fromVertex.X);
                double labelX = (fromVertex.X + toVertex.X) / 2;
                double labelY = (fromVertex.Y + toVertex.Y) / 2;

                // Смещаем вес перпендикулярно линии ребра
                labelX += 10 * Math.Sin(labelAngle);
                labelY -= 10 * Math.Cos(labelAngle);

                Canvas.SetLeft(label, labelX);
                Canvas.SetTop(label, labelY);

                _canvas.Children.Add(label);

                string edgeKey = $"{edge.From}-{edge.To}";
                _edgeShapes[edgeKey] = line;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в DrawEdge: {ex.Message}");
            }
        }

        private bool IsEdgeInPath(int from, int to)
        {
            for (int i = 0; i < _currentPath.Count - 1; i++)
            {
                if ((_currentPath[i] == from && _currentPath[i + 1] == to) ||
                    (_currentPath[i] == to && _currentPath[i + 1] == from))
                {
                    return true;
                }
            }
            return false;
        }

        public void HighlightPath(List<int> path)
        {
            try
            {
                if (path == null)
                {
                    _currentPath.Clear();
                    DrawGraph();
                    return;
                }

                _currentPath = new List<int>(path);
                DrawGraph(); // Перерисовываем весь граф с подсветкой
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в HighlightPath: {ex.Message}");
            }
        }

        public void ClearPath()
        {
            try
            {
                _currentPath.Clear();
                DrawGraph();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в ClearPath: {ex.Message}");
            }
        }

        private string FormatWeight(double weight)
        {
            return weight.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        }

        private void MakeDraggable(Ellipse ellipse, Vertex vertex)
        {
            bool isDragging = false;

            ellipse.MouseLeftButtonDown += (sender, e) =>
            {
                isDragging = true;
                ellipse.CaptureMouse();
                ellipse.Fill = Brushes.LightCoral;
            };

            ellipse.MouseMove += (sender, e) =>
            {
                if (isDragging)
                {
                    var position = e.GetPosition(_canvas);
                    vertex.X = position.X;
                    vertex.Y = position.Y;

                    Canvas.SetLeft(ellipse, position.X - 20);
                    Canvas.SetTop(ellipse, position.Y - 20);

                    if (_vertexLabels.ContainsKey(vertex.Id))
                    {
                        Canvas.SetLeft(_vertexLabels[vertex.Id], position.X - 15);
                        Canvas.SetTop(_vertexLabels[vertex.Id], position.Y - 10);
                    }

                    RedrawAllEdges();
                }
            };

            ellipse.MouseLeftButtonUp += (sender, e) =>
            {
                isDragging = false;
                ellipse.ReleaseMouseCapture();
                // Возвращаем цвет в зависимости от того, часть ли это пути
                ellipse.Fill = _currentPath.Contains(vertex.Id) ? _pathVertexBrush : _vertexFill;
            };
        }

        private void RedrawAllEdges()
        {
            try
            {
                if (_canvas == null || _graph == null)
                    return;

                var edgesToRemove = _canvas.Children
                    .OfType<Line>()
                    .Where(l => l.Tag?.ToString() == "edge" || l.Tag?.ToString() == "arrow")
                    .ToList();

                var labelsToRemove = _canvas.Children
                    .OfType<TextBlock>()
                    .Where(tb => tb.Tag?.ToString() == "edge-label")
                    .ToList();

                foreach (var element in edgesToRemove.Concat<UIElement>(labelsToRemove))
                {
                    _canvas.Children.Remove(element);
                }

                _edgeShapes.Clear();

                // Используем ту же логику предотвращения дублирования, что и в DrawGraph
                var drawnEdges = new HashSet<string>();
                
                foreach (var edge in _graph.Edges)
                {
                    // Для неориентированных рёбер рисуем только одно направление
                    if (!edge.IsDirected)
                    {
                        string edgeKey = edge.From < edge.To ? $"{edge.From}-{edge.To}" : $"{edge.To}-{edge.From}";
                        if (drawnEdges.Contains(edgeKey))
                            continue;
                        drawnEdges.Add(edgeKey);
                    }
                    else
                    {
                        // Для ориентированных рёбер проверяем, есть ли обратное ребро с тем же весом
                        // Если есть - обрабатываем как неориентированное (одна линия, без стрелок, один вес)
                        var reverseEdge = _graph.Edges.FirstOrDefault(e => 
                            e.From == edge.To && e.To == edge.From && 
                            e.IsDirected && Math.Abs(e.Weight - edge.Weight) < 0.0001);
                        
                        if (reverseEdge != null)
                        {
                            // Это двустороннее ребро с одинаковым весом - рисуем как неориентированное
                            string edgeKey = edge.From < edge.To ? $"{edge.From}-{edge.To}" : $"{edge.To}-{edge.From}";
                            if (drawnEdges.Contains(edgeKey))
                                continue;
                            drawnEdges.Add(edgeKey);
                            
                            // Создаём временное неориентированное ребро для отрисовки
                            var undirectedEdge = new Edge(edge.From, edge.To, edge.Weight, false);
                            DrawEdge(undirectedEdge);
                            continue;
                        }
                        else
                        {
                            // Обычное ориентированное ребро
                            string edgeKey = $"{edge.From}-{edge.To}";
                            if (drawnEdges.Contains(edgeKey))
                                continue;
                            drawnEdges.Add(edgeKey);
                        }
                    }
                    
                    DrawEdge(edge);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в RedrawAllEdges: {ex.Message}");
            }
        }

        // === Публичные методы ===

        public void AddVertex(string name)
        {
            _graph.AddVertex(name);
            DrawGraph();
        }

        public void AddEdge(int from, int to, double weight, bool isDirected = false)
        {
            if (from < 0 || from >= _graph.Vertices.Count ||
                to < 0 || to >= _graph.Vertices.Count)
            {
                throw new ArgumentException("Неверные номера вершин");
            }

            _graph.AddEdge(from, to, weight, isDirected);
            DrawGraph();
        }

        public List<int> FindShortestPath(int start, int end)
        {
            return _graph.FindShortestPath(start, end);
        }

        public void BuildFromMatrix(string matrixText)
        {
            try
            {
                var lines = matrixText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                int n = lines.Length;
                double[,] matrix = new double[n, n];

                for (int i = 0; i < n; i++)
                {
                    var values = lines[i].Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int j = 0; j < n; j++)
                    {
                        if (j < values.Length && values[j].Trim() != "")
                        {
                            if (double.TryParse(values[j].Replace(',', '.'),
                                System.Globalization.NumberStyles.Any,
                                System.Globalization.CultureInfo.InvariantCulture,
                                out double weight))
                            {
                                matrix[i, j] = weight;
                            }
                            else
                            {
                                matrix[i, j] = 0;
                            }
                        }
                    }
                }

                _graph.Clear();
                _currentPath.Clear();

                for (int i = 0; i < n; i++)
                {
                    _graph.AddVertex($"V{i}");
                }

                for (int i = 0; i < n; i++)
                {
                    for (int j = 0; j < n; j++)
                    {
                        if (matrix[i, j] != 0)
                        {
                            bool isDirected = (matrix[i, j] != matrix[j, i]);
                            // Для неориентированных графов обрабатываем только верхнюю треугольную часть
                            // чтобы избежать дублирования (AddEdge сам добавит обратное ребро)
                            if (!isDirected && i > j)
                            {
                                continue; // Пропускаем нижнюю треугольную часть для неориентированных графов
                            }
                            _graph.AddEdge(i, j, matrix[i, j], isDirected);
                        }
                    }
                }

                DrawGraph();
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Ошибка парсинга матрицы: {ex.Message}");
            }
        }

        public void BuildFromAdjacencyList(string listText)
        {
            try
            {
                _graph.Clear();
                _currentPath.Clear();

                var adjacencyList = new Dictionary<int, List<(int to, double weight)>>();
                var lines = listText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                var vertexIds = new HashSet<int>();

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var parts = line.Split(new[] { ':' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 1)
                    {
                        if (int.TryParse(parts[0].Trim(), out int fromVertex))
                        {
                            vertexIds.Add(fromVertex);

                            if (!adjacencyList.ContainsKey(fromVertex))
                                adjacencyList[fromVertex] = new List<(int, double)>();

                            if (parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]))
                            {
                                string edgesText = parts[1];

                                // Поддерживаем оба символа стрелки: → и ->
                                var matches = System.Text.RegularExpressions.Regex.Matches(
                                    edgesText,
                                    @"(?:→|->)\s*(\d+)(?:\(\s*([\d\.,]+)\s*\))?");

                                foreach (System.Text.RegularExpressions.Match match in matches)
                                {
                                    if (match.Groups[1].Success)
                                    {
                                        int toVertex = int.Parse(match.Groups[1].Value);
                                        vertexIds.Add(toVertex);

                                        double weight = 1.0;
                                        if (match.Groups[2].Success)
                                        {
                                            string weightStr = match.Groups[2].Value.Replace('.', ',');
                                            if (double.TryParse(weightStr, out double parsedWeight))
                                            {
                                                weight = parsedWeight;
                                            }
                                        }

                                        adjacencyList[fromVertex].Add((toVertex, weight));
                                    }
                                }
                            }
                        }
                    }
                }

                int maxVertexId = vertexIds.Any() ? vertexIds.Max() : 0;
                for (int i = 0; i <= maxVertexId; i++)
                {
                    if (vertexIds.Contains(i))
                    {
                        _graph.AddVertex($"V{i}");
                    }
                }

                // Определяем, является ли граф неориентированным
                // Проверяем симметричность списка смежности
                // Граф считается неориентированным, если для каждого ребра (i,j) с весом w
                // существует обратное ребро (j,i) с тем же весом w
                bool isUndirected = true;
                if (adjacencyList.Count > 0)
                {
                    foreach (var fromVertex in adjacencyList.Keys)
                    {
                        foreach (var (to, weight) in adjacencyList[fromVertex])
                        {
                            // Проверяем, есть ли обратное ребро с тем же весом
                            if (!adjacencyList.ContainsKey(to))
                            {
                                isUndirected = false;
                                break;
                            }
                            
                            // Ищем обратное ребро
                            bool foundReverse = false;
                            foreach (var (reverseTo, reverseWeight) in adjacencyList[to])
                            {
                                if (reverseTo == fromVertex && Math.Abs(reverseWeight - weight) < 0.0001)
                                {
                                    foundReverse = true;
                                    break;
                                }
                            }
                            
                            if (!foundReverse)
                            {
                                isUndirected = false;
                                break;
                            }
                        }
                        if (!isUndirected) break;
                    }
                }
                else
                {
                    // Пустой граф считаем неориентированным
                    isUndirected = true;
                }

                // Если граф неориентированный, обрабатываем только одну сторону (from < to)
                // чтобы избежать дублирования (AddEdge сам добавит обратное ребро)
                foreach (var fromVertex in adjacencyList.Keys)
                {
                    foreach (var (to, weight) in adjacencyList[fromVertex])
                    {
                        if (fromVertex <= maxVertexId && to <= maxVertexId)
                        {
                            if (isUndirected && fromVertex > to)
                            {
                                // Пропускаем нижнюю треугольную часть для неориентированных графов
                                continue;
                            }
                            _graph.AddEdge(fromVertex, to, weight, !isUndirected);
                        }
                    }
                }

                DrawGraph();
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Ошибка парсинга списка смежности: {ex.Message}");
            }
        }

        // В GraphVisualizerService.cs добавьте:
        public double GetEdgeWeight(int from, int to)
        {
            var edge = _graph.Edges.FirstOrDefault(e =>
                (e.From == from && e.To == to) ||
                (!e.IsDirected && e.From == to && e.To == from));

            return edge?.Weight ?? 0;
        }

        public int GetVertexCount() => _graph.Vertices.Count;

        public int GetEdgeCount() => _graph.Edges.Count;

        public List<string> GetVertexNames() => _graph.Vertices.Select(v => v.Name).ToList();

        public void ClearGraph()
        {
            _graph.Clear();
            _currentPath.Clear();
            DrawGraph();
        }

        public string GetAdjacencyMatrixText()
        {
            var matrix = _graph.GetAdjacencyMatrix();
            int n = _graph.Vertices.Count;

            if (n == 0) return "Граф пуст";

            var sb = new System.Text.StringBuilder();

            sb.Append("     ");
            for (int i = 0; i < n; i++)
            {
                sb.AppendFormat("{0,5}", _graph.Vertices[i].Name);
            }
            sb.AppendLine();
            sb.AppendLine(new string('-', 6 + n * 5));

            for (int i = 0; i < n; i++)
            {
                sb.AppendFormat("{0,4} |", _graph.Vertices[i].Name);
                for (int j = 0; j < n; j++)
                {
                    if (matrix[i, j] == 0)
                    {
                        sb.AppendFormat("{0,5}", "0");
                    }
                    else if (matrix[i, j] == 1.0)
                    {
                        sb.AppendFormat("{0,5}", "1");
                    }
                    else
                    {
                        sb.AppendFormat("{0,5:0.#}", matrix[i, j]);
                    }
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        public string GetAdjacencyListText()
        {
            var adjacencyList = _graph.GetAdjacencyList();

            if (_graph.Vertices.Count == 0) return "Граф пуст";

            var sb = new System.Text.StringBuilder();

            for (int i = 0; i < _graph.Vertices.Count; i++)
            {
                sb.AppendFormat("{0,2} ({1}): ", i, _graph.Vertices[i].Name);

                if (adjacencyList.ContainsKey(i) && adjacencyList[i].Count > 0)
                {
                    bool first = true;
                    foreach (var (to, weight) in adjacencyList[i])
                    {
                        if (!first) sb.Append(" ");
                        first = false;

                        if (weight == 1.0)
                        {
                            sb.AppendFormat("→{0}", to);
                        }
                        else
                        {
                            sb.AppendFormat("→{0}({1:0.#})", to, weight);
                        }
                    }
                }
                else
                {
                    sb.Append("нет смежных вершин");
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}