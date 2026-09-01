#r "nuget: Npgsql, 8.0.3"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Npgsql;

var connString = "Host=localhost;Port=5434;Database=UnifiedCommerceDb;Username=postgres;Password=Nive@123";
using (var conn = new NpgsqlConnection(connString))
{
    conn.Open();

    var validCodes = string.Join(", ", System.Linq.Enumerable.Range(1, 16).Select(i => $"'MER-{i:D3}'")) + ", 'MER-030'";
    var pQuery = $"SELECT id FROM products WHERE product_code NOT IN ({validCodes})";

    var queries = new List<string>
    {
        $"DELETE FROM product_categories WHERE product_id IN ({pQuery});",
        $"DELETE FROM product_channel_visibility WHERE product_id IN ({pQuery});",
        $"DELETE FROM product_inventory_settings WHERE product_id IN ({pQuery});",
        $"DELETE FROM product_images WHERE product_id IN ({pQuery});",
        $"DELETE FROM product_tax_assignments WHERE product_id IN ({pQuery});",
        $"DELETE FROM product_barcodes WHERE product_id IN ({pQuery});",
        $"DELETE FROM product_attribute_values WHERE product_id IN ({pQuery});",
        $"DELETE FROM product_reviews WHERE product_id IN ({pQuery});",
        $"DELETE FROM price_list_items WHERE product_id IN ({pQuery});",
        // OMITTED product_recommendation_links let dynamic handle it
        $"DELETE FROM product_variant_option_values WHERE product_variant_id IN (SELECT id FROM product_variants WHERE product_id IN ({pQuery}));",
        $"DELETE FROM inventory_balances WHERE product_variant_id IN (SELECT id FROM product_variants WHERE product_id IN ({pQuery}));",
        $"DELETE FROM sales_order_lines WHERE product_variant_id IN (SELECT id FROM product_variants WHERE product_id IN ({pQuery}));",
        $"DELETE FROM shopping_cart_item_options WHERE shopping_cart_item_id IN (SELECT id FROM shopping_cart_items WHERE product_variant_id IN (SELECT id FROM product_variants WHERE product_id IN ({pQuery})));",
        $"DELETE FROM shopping_cart_items WHERE product_variant_id IN (SELECT id FROM product_variants WHERE product_id IN ({pQuery}));",
        $"DELETE FROM checkout_session_line_options WHERE checkout_session_line_id IN (SELECT id FROM checkout_session_lines WHERE product_variant_id IN (SELECT id FROM product_variants WHERE product_id IN ({pQuery})));",
        $"DELETE FROM checkout_session_lines WHERE product_variant_id IN (SELECT id FROM product_variants WHERE product_id IN ({pQuery}));",
        $"DELETE FROM product_choice_options WHERE product_choice_group_id IN (SELECT id FROM product_choice_groups WHERE product_id IN ({pQuery}));",
        $"DELETE FROM product_choice_groups WHERE product_id IN ({pQuery});",
        $"DELETE FROM product_variants WHERE product_id IN ({pQuery});",
        $"DELETE FROM product_option_values WHERE product_option_id IN (SELECT id FROM product_options WHERE product_id IN ({pQuery}));",
        $"DELETE FROM product_options WHERE product_id IN ({pQuery});",
        $"DELETE FROM products WHERE product_code NOT IN ({validCodes});"
    };

    bool success = false;
    int maxAttempts = 50;

    while (!success && maxAttempts > 0)
    {
        maxAttempts--;
        using (var transaction = conn.BeginTransaction())
        {
            try 
            {
                foreach (var q in queries)
                {
                    using (var cmd = new NpgsqlCommand(q, conn)) {
                        int count = cmd.ExecuteNonQuery();
                    }
                }
                transaction.Commit();
                Console.WriteLine("Done deleting products and related data.");
                success = true;
            }
            catch (PostgresException ex) when (ex.SqlState == "23503" || ex.SqlState == "23001")
            {
                transaction.Rollback();
                
                var match = Regex.Match(ex.Message, @"violates (?:RESTRICT setting of )?foreign key constraint ""([^""]+)"" on table ""([^""]+)""");
                if (match.Success)
                {
                    var constraint = match.Groups[1].Value;
                    var table = match.Groups[2].Value;
                    
                    var fkQuery = @"
                        SELECT kcu.column_name, ccu.table_name AS ref_table, ccu.column_name AS ref_col
                        FROM information_schema.table_constraints tc
                        JOIN information_schema.key_column_usage kcu ON tc.constraint_name = kcu.constraint_name
                        JOIN information_schema.constraint_column_usage ccu ON ccu.constraint_name = tc.constraint_name
                        WHERE tc.constraint_name = @cname AND tc.table_name = @tname;
                    ";
                    
                    string column = null;
                    string refTable = null;
                    string refCol = null;
                    using (var cmd = new NpgsqlCommand(fkQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("cname", constraint);
                        cmd.Parameters.AddWithValue("tname", table);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                column = reader.GetString(0);
                                refTable = reader.GetString(1);
                                refCol = reader.GetString(2);
                            }
                        }
                    }

                    if (column != null)
                    {
                        var refDeleteStmt = queries.FirstOrDefault(s => s.StartsWith($"DELETE FROM {refTable} WHERE") || s.StartsWith($"DELETE FROM \"{refTable}\" WHERE"));
                        
                        if (refDeleteStmt != null)
                        {
                            var whereClause = refDeleteStmt.Substring(refDeleteStmt.IndexOf("WHERE"));
                            whereClause = whereClause.TrimEnd(';');
                            
                            var newDelete = $"DELETE FROM \"{table}\" WHERE \"{column}\" IN (SELECT \"{refCol}\" FROM \"{refTable}\" {whereClause});";
                            
                            if (!queries.Contains(newDelete))
                            {
                                queries.Insert(queries.Count - 1, newDelete);
                                Console.WriteLine($"Added missing rule: {newDelete}");
                            }
                            else 
                            {
                                Console.WriteLine($"Rule already exists! Cyclic dependency or same failure? {newDelete}");
                                break;
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Could not find delete statement for referenced table {refTable}");
                            break;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Could not find column for constraint {constraint}");
                        break;
                    }
                }
                else
                {
                    Console.WriteLine($"Could not parse FK error: {ex.Message}");
                    break;
                }
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"Error: {ex.Message}");
                break;
            }
        }
    }
}
