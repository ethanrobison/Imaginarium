﻿#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Noun.cs" company="Ian Horswill">
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

using System.Collections.Generic;
using System.Diagnostics;

/// <summary>
/// A monadic concept that can be realized in English as the head of an NP.
/// </summary>
[DebuggerDisplay("{" + nameof(Text) + "}")]
public abstract class Noun : MonadicConcept
{
    static Noun()
    {
        Ontology.AllReferentTables.Add(AllNouns);
    }

    protected Noun(string[] name) : base(name) 
    { }

    public static Dictionary<TokenString, Noun> AllNouns = new Dictionary<TokenString, Noun>();

    /// <summary>
    /// Returns the noun named by the specified token string, or null if there is none.
    /// </summary>
    public static Noun Find(params string[] tokens) => AllNouns.LookupOrDefault(tokens);


 }