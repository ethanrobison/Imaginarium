﻿#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Verb.cs" company="Ian Horswill">
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

/// <summary>
/// Represents a verb, i.e. a binary relation
/// </summary>
public class Verb : Concept
{
    /// <summary>
    /// Verbs that are implied by this verb
    /// </summary>
    public List<Verb> Generalizations = new List<Verb>();

    /// <summary>
    /// Verbs that are mutually exclusive with this one: A this B implies not A exclusion B
    /// </summary>
    public List<Verb> MutualExclusions = new List<Verb>();

    /// <summary>
    /// There is at most one object for each possible subject
    /// </summary>
    public bool IsFunction;
    /// <summary>
    /// There is an object for every possible subject.
    /// </summary>
    public bool IsTotal;

    public bool IsReflexive;

    public bool IsAntiReflexive;

    public bool IsSymmetric;

    public bool IsAntiSymmetric;

    /// <summary>
    /// The initial probability of the relation.
    /// </summary>
    public float Density = 0.5f;

    public static readonly TokenTrie<Verb> Trie = new TokenTrie<Verb>();

    public static IEnumerable<Verb> AllVerbs => Trie.Contents;

    public override bool IsNamed(string[] tokens) => tokens.SameAs(SingularForm) || tokens.SameAs(PluralForm);

    public override string[] StandardName => SingularForm;

    // ReSharper disable InconsistentNaming
    private string[] _singular, _plural;
    // ReSharper restore InconsistentNaming

    /// <summary>
    /// Singular form of the verb
    /// </summary>
    public string[] SingularForm
    {
        get
        {
            EnsureSingularForm();
            return _singular;
        }
        set
        {
            if (_singular != null) Trie.Store(_singular, null);
            _singular = value;
            Trie.Store(_singular, this);
            EnsurePluralForm();
            CreateGerundForms();
        }
    }

    /// <summary>
    /// Add likely spellings of the gerund of this verb.
    /// They are stored as if they are plural inflections.
    /// </summary>
    private void CreateGerundForms()
    {
        foreach (var form in Inflection.GerundOfVerb(_plural))
            Trie.Store(form, this, true);
    }

    /// <summary>
    /// Make sure the noun has a singular verb.
    /// </summary>
    private void EnsureSingularForm()
    {
        if (_singular == null)
            SingularForm = Inflection.SingularOfVerb(_plural);
    }

    /// <summary>
    /// Plural form of the verb
    /// </summary>
    public string[] PluralForm
    {
        get
        {
            EnsurePluralForm();
            return _plural;
        }
        set
        {
            if (_plural != null) Trie.Store(_plural, null);
            _plural = value;
            Trie.Store(_plural, this, true);
            EnsureSingularForm();
        }
    }

    public CommonNoun SubjectKind { get; set; }
    public CommonNoun ObjectKind { get; set; }

    private void EnsurePluralForm()
    {
        if (_plural == null)
            PluralForm = Inflection.PluralOfVerb(_singular);
    }

    public static Verb Find(params string[] tokens)
    {
        int index = 0;
        var v = Trie.Lookup(tokens, ref index);
        if (index != tokens.Length)
            return null;
        return v;
    }
}
