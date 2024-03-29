﻿using System.Linq;

namespace VersionOne.TeamSync.JiraConnector
{
    public static class ReservedWords
    {
        private static readonly string[] Reserved = {
            "abort", "access", "add", "after", "alias", "all", "alter", "and", "any", "as", "asc",
            "audit", "avg", "before", "begin", "between", "boolean", "break", "by", "byte", "catch", "cf",
            "char", "character", "check", "checkpoint", "collate", "collation", "column", "commit", "connect",
            "continue",
            "count", "create", "current", "date", "decimal", "declare", "decrement", "default", "defaults", "define",
            "delete",
            "delimiter", "desc", "difference", "distinct", "divide", "do", "double", "drop", "else", "empty", "encoding",
            "end", "equals", "escape", "exclusive", "exec", "execute", "exists", "explain", "false", "fetch", "file",
            "field",
            "first", "float", "for", "from", "function", "go", "goto", "grant", "greater", "group", "having",
            "identified", "if", "immediate", "in", "increment", "index", "initial", "inner", "inout", "input", "insert",
            "int", "integer", "intersect", "intersection", "into", "is", "isempty", "isnull", "join", "last", "left",
            "less", "like", "limit", "lock", "long", "max", "min", "minus", "mode", "modify",
            "modulo", "more", "multiply", "next", "noaudit", "not", "notin", "nowait", "null", "number", "object",
            "of", "on", "option", "or", "order", "outer", "output", "power", "previous", "prior", "privileges",
            "public", "raise", "raw", "remainder", "rename", "resource", "return", "returns", "revoke", "right", "row",
            "rowid", "rownum", "rows", "select", "session", "set", "share", "size", "sqrt", "start", "strict",
            "string", "subtract", "sum", "synonym", "table", "then", "to", "trans", "transaction", "trigger", "true",
            "uid", "union", "unique", "update", "user", "validate", "values", "view", "when", "whenever", "where",
            "while", "with"
        };

        private const string InQuotes = "\"{0}\"";

        public static bool IsReserved(this string value)
        {
            return Reserved.Contains(value);
        }

        public static string QuoteReservedWord(this string value)
        {
            if (Reserved.Contains(value.ToLower()))
                return string.Format(InQuotes, value);
            return value;
        }
    }
}
