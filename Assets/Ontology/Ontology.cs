﻿#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Ontology.cs" company="Ian Horswill">
// Copyright (C) 2019 Ian Horswill
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in the
// Software without restriction, including without limitation the rights to use, copy,
// modify, merge, publish, distribute, sublicense, and/or sell copies of the Software,
// and to permit persons to whom the Software is furnished to do so, subject to the
// following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
// PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Operations for accessing the ontology as a whole
/// The ontology consists of all the Referent objects and the information within them (e.g. Property objects)
/// </summary>
public static class Ontology
{
    /// <summary>
    /// List of all the tables of different kinds of referents.
    /// Used so we know what to clear when reinitializing the ontology.
    /// </summary>
    public static readonly List<IDictionary> AllReferentTables = new List<IDictionary>();

    /// <summary>
    /// Return true if there's already a concept with the specified name.
    /// </summary>
    public static object Find(TokenString name)
    {
        var dict = AllReferentTables.FirstOrDefault(t => t.Contains(name));
        var result = dict?[name];
        if (result == null)
            foreach (var t in TokenTrieBase.AllTokenTries)
            {
                result = t.Find(name);
                if (result != null)
                    break;
            }

        return result;
    }

    /// <summary>
    /// Removes all concepts form the ontology.
    /// </summary>
    public static void EraseConcepts()
    {
        foreach (var c in AllReferentTables)
            c.Clear();
        
        TokenTrieBase.ClearAllTries();
    }

    public static void EnsureUndefinedOrDefinedAsType(string[] name, Type newType)
    {
        if (name == null)
            return;
        var old = Find(name);
        if (old != null && old.GetType() != newType)
            throw new NameCollisionException(name, old.GetType(), newType);
    }
}
