using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphVisualizer.Models
{
    public class Vertex
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double X { get; set; }
        public double Y { get; set; }

        public Vertex(int id, string name)
        {
            Id = id;
            Name = name;
            X = 0;
            Y = 0;
        }
    }

    public class Edge
    {
        public int From { get; set; }
        public int To { get; set; }
        public double Weight { get; set; }
        public bool IsDirected { get; set; }

        public Edge(int from, int to, double weight, bool isDirected = false)
        {
            From = from;
            To = to;
            Weight = weight;
            IsDirected = isDirected;
        }
    }

    public class Graph
    {
        public List<Vertex> Vertices { get; set; }
        public List<Edge> Edges { get; set; }

        public Graph()
        {
            Vertices = new List<Vertex>();
            Edges = new List<Edge>();
        }

        // Определяет, является ли граф ориентированным
        // Граф считается ориентированным, если есть хотя бы одно ориентированное ребро
        // или матрица смежности несимметрична
        public bool IsDirected()
        {
            // Проверяем, есть ли хотя бы одно ориентированное ребро
            if (Edges.Any(e => e.IsDirected))
                return true;

            // Проверяем симметричность матрицы смежности
            var matrix = GetAdjacencyMatrix();
            int n = Vertices.Count;

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    // Если есть ребро в одном направлении, но нет в обратном - граф ориентированный
                    if (matrix[i, j] != 0 && matrix[j, i] == 0)
                        return true;
                    // Если веса не совпадают - граф ориентированный
                    if (matrix[i, j] != 0 && matrix[j, i] != 0 && Math.Abs(matrix[i, j] - matrix[j, i]) > 0.0001)
                        return true;
                }
            }

            return false;
        }

        public void AddVertex(string name)
        {
            int id = Vertices.Count;
            var vertex = new Vertex(id, name);
            Vertices.Add(vertex);
            ArrangeVerticesOnCircle();
        }

        private void ArrangeVerticesOnCircle()
        {
            if (Vertices.Count == 0) return;

            double centerX = 300;
            double centerY = 200;
            double radius = Math.Min(150, Vertices.Count * 20);

            for (int i = 0; i < Vertices.Count; i++)
            {
                double angle = 2 * Math.PI * i / Vertices.Count;
                Vertices[i].X = centerX + radius * Math.Cos(angle);
                Vertices[i].Y = centerY + radius * Math.Sin(angle);
            }
        }

        public Vertex GetVertex(int id)
        {
            return Vertices.FirstOrDefault(v => v.Id == id);
        }

        public void BuildFromAdjacencyMatrix(double[,] matrix, List<string> vertexNames = null)
        {
            Clear();

            int n = matrix.GetLength(0);

            for (int i = 0; i < n; i++)
            {
                string name = vertexNames != null && i < vertexNames.Count ? vertexNames[i] : $"V{i}";
                AddVertex(name);
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
                        AddEdge(i, j, matrix[i, j], isDirected);
                    }
                }
            }
        }

        public void BuildFromAdjacencyList(Dictionary<int, List<(int to, double weight)>> adjacencyList,
                                          List<string> vertexNames = null)
        {
            Clear();

            int maxVertexId = adjacencyList.Keys.Any() ?
                Math.Max(adjacencyList.Keys.Max(),
                        adjacencyList.Values.SelectMany(v => v.Select(e => e.to)).Max()) : 0;

            for (int i = 0; i <= maxVertexId; i++)
            {
                string name = vertexNames != null && i < vertexNames.Count ? vertexNames[i] : $"V{i}";
                AddVertex(name);
            }

            foreach (var fromVertex in adjacencyList)
            {
                foreach (var (to, weight) in fromVertex.Value)
                {
                    if (fromVertex.Key <= maxVertexId && to <= maxVertexId)
                    {
                        AddEdge(fromVertex.Key, to, weight, true);
                    }
                }
            }
        }

        public void AddEdge(int from, int to, double weight = 1.0, bool isDirected = false)
        {
            // Проверяем валидность индексов вершин
            if (from < 0 || from >= Vertices.Count || to < 0 || to >= Vertices.Count)
            {
                throw new ArgumentException($"Неверные индексы вершин: from={from}, to={to}, количество вершин={Vertices.Count}");
            }

            // Проверяем валидность веса
            if (double.IsNaN(weight) || double.IsInfinity(weight) || weight < 0)
            {
                throw new ArgumentException($"Неверный вес ребра: {weight}");
            }

            // Проверяем существующие рёбра в обоих направлениях
            var existingEdge = Edges.FirstOrDefault(e => e.From == from && e.To == to);

            if (existingEdge != null)
            {
                // Обновляем существующее ребро
                existingEdge.Weight = weight;
                existingEdge.IsDirected = isDirected;
            }
            else
            {
                // Добавляем новое ребро
                Edges.Add(new Edge(from, to, weight, isDirected));
            }

            // Если неориентированное - добавляем обратное ребро
            if (!isDirected)
            {
                var reverseEdge = Edges.FirstOrDefault(e => e.From == to && e.To == from);
                if (reverseEdge == null)
                {
                    Edges.Add(new Edge(to, from, weight, false));
                }
                else
                {
                    reverseEdge.Weight = weight;
                    reverseEdge.IsDirected = false;
                }
            }
        }

        public void Clear()
        {
            Vertices.Clear();
            Edges.Clear();
        }

        // Алгоритм Дейкстры для поиска кратчайшего пути
        public List<int> FindShortestPath(int start, int end)
        {
            try
            {
                if (start < 0 || start >= Vertices.Count || end < 0 || end >= Vertices.Count)
                    return new List<int>();

                if (start == end)
                    return new List<int> { start };

                // Инициализация
                int n = Vertices.Count;
                double[] distances = new double[n];
                int[] previous = new int[n];
                bool[] visited = new bool[n];

                for (int i = 0; i < n; i++)
                {
                    distances[i] = double.PositiveInfinity;
                    previous[i] = -1;
                    visited[i] = false;
                }

                distances[start] = 0;

                // Основной цикл
                for (int i = 0; i < n; i++)
                {
                    // Находим непосещенную вершину с минимальным расстоянием
                    int current = -1;
                    double minDistance = double.PositiveInfinity;

                    for (int j = 0; j < n; j++)
                    {
                        if (!visited[j] && distances[j] < minDistance)
                        {
                            minDistance = distances[j];
                            current = j;
                        }
                    }

                    if (current == -1 || distances[current] == double.PositiveInfinity)
                        break; // Нет достижимых вершин

                    if (current == end)
                        break; // Достигли конечной вершины

                    visited[current] = true;

                    // Обновляем расстояния до соседей
                    // Для неориентированных графов AddEdge уже создаёт обратные рёбра,
                    // поэтому достаточно проверять только рёбра, где From == current
                    foreach (var edge in Edges.Where(e => e.From == current))
                    {
                        int neighbor = edge.To;
                        
                        // Проверяем валидность индекса соседа
                        if (neighbor < 0 || neighbor >= n)
                            continue;

                        // Проверяем, что вес ребра валиден
                        if (double.IsNaN(edge.Weight) || double.IsInfinity(edge.Weight) || edge.Weight < 0)
                            continue;

                        if (visited[neighbor])
                            continue; // Уже обработали эту вершину

                        double newDistance = distances[current] + edge.Weight;

                        // Проверяем на переполнение
                        if (double.IsInfinity(distances[current]) || double.IsInfinity(edge.Weight))
                            continue;

                        if (newDistance < distances[neighbor])
                        {
                            distances[neighbor] = newDistance;
                            previous[neighbor] = current;
                        }
                    }
                }

                // Восстанавливаем путь
                if (distances[end] == double.PositiveInfinity)
                    return new List<int>(); // Пути нет

                List<int> path = new List<int>();
                int node = end;

                while (node != -1 && node >= 0 && node < n)
                {
                    path.Insert(0, node);
                    node = previous[node];
                    
                    // Защита от циклов
                    if (path.Count > n)
                        return new List<int>();
                }

                return path;
            }
            catch (Exception)
            {
                return new List<int>();
            }
        }

        public double[,] GetAdjacencyMatrix()
        {
            int n = Vertices.Count;
            double[,] matrix = new double[n, n];

            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    matrix[i, j] = 0;

            foreach (var edge in Edges)
            {
                matrix[edge.From, edge.To] = edge.Weight;
            }

            return matrix;
        }

        public Dictionary<int, List<(int to, double weight)>> GetAdjacencyList()
        {
            var adjacencyList = new Dictionary<int, List<(int, double)>>();

            for (int i = 0; i < Vertices.Count; i++)
            {
                adjacencyList[i] = new List<(int, double)>();
            }

            foreach (var edge in Edges)
            {
                adjacencyList[edge.From].Add((edge.To, edge.Weight));
            }

            return adjacencyList;
        }
    }
}