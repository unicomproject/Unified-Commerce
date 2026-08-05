using System.Collections.Concurrent;

namespace E_POS.Infrastructure.Modules.ECommerce.Storefront.Services.Autocomplete;

public sealed class AutocompleteItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public string Type { get; set; } = "Product";
    public int Popularity { get; set; }
}

public sealed class TrieNode
{
    public ConcurrentDictionary<char, TrieNode> Children { get; } = new();
    public bool IsEndOfWord { get; set; }
    public List<AutocompleteItem> Items { get; } = [];
}

public sealed class Trie
{
    private readonly TrieNode _root = new();
    private readonly object _lock = new();

    public void Insert(string text, AutocompleteItem item)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        lock (_lock)
        {
            var current = _root;
            foreach (var ch in text.Trim())
            {
                var key = char.ToUpperInvariant(ch);
                if (!current.Children.TryGetValue(key, out var nextNode))
                {
                    nextNode = new TrieNode();
                    current.Children[key] = nextNode;
                }

                current = nextNode;
            }

            current.IsEndOfWord = true;
            if (!current.Items.Any(x => x.Id == item.Id && x.Type == item.Type))
            {
                current.Items.Add(item);
            }
        }
    }

    public List<AutocompleteItem> Search(string prefix, int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return [];
        }

        var current = _root;
        foreach (var ch in prefix.Trim())
        {
            var key = char.ToUpperInvariant(ch);
            if (!current.Children.TryGetValue(key, out var nextNode))
            {
                return [];
            }

            current = nextNode;
        }

        var results = new List<AutocompleteItem>();
        CollectWords(current, results, limit);

        return results
            .GroupBy(x => new { x.Type, x.Id })
            .Select(x => x.First())
            .OrderByDescending(x => x.Popularity)
            .ThenBy(x => x.Name)
            .Take(limit)
            .ToList();
    }

    private static void CollectWords(TrieNode node, List<AutocompleteItem> results, int limit)
    {
        if (results.Count >= limit)
        {
            return;
        }

        if (node.IsEndOfWord)
        {
            results.AddRange(node.Items);
        }

        foreach (var child in node.Children.Values)
        {
            CollectWords(child, results, limit);
            if (results.Count >= limit)
            {
                break;
            }
        }
    }
}