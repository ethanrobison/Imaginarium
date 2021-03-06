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
using System.Linq;
using UnityEngine;

/// <summary>
/// Represents a verb, i.e. a binary relation
/// </summary>
public class Verb : Concept
{
    public Verb() : base(null)
    { }

    /// <summary>
    /// Verbs that are implied by this verb
    /// </summary>
    public List<Verb> Generalizations = new List<Verb>();

    /// <summary>
    /// Verbs that are mutually exclusive with this one: A this B implies not A exclusion B
    /// </summary>
    public List<Verb> MutualExclusions = new List<Verb>();

    public List<Verb> Subspecies = new List<Verb>();
    // ReSharper disable once IdentifierTypo
    public List<Verb> Superspecies = new List<Verb>();

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

    public static IEnumerable<Verb> AllVerbs => Trie.Contents.Distinct();

    public override bool IsNamed(string[] tokens) => tokens.SameAs(SingularForm) || tokens.SameAs(PluralForm);

    // ReSharper disable InconsistentNaming
    private string[] _baseForm;
    private string[] _gerundForm;
    // ReSharper restore InconsistentNaming

    public string[] BaseForm
    {
        get => _baseForm;
        set
        {
            _baseForm = value;
            Trie.Store(value, this);
            EnsureGerundForm();
            EnsurePluralForm();
            EnsureSingularForm();
        }
    }

    public string[] GerundForm
    {
        get => _gerundForm;
        set
        {
            _gerundForm = value;
            Trie.Store(value, this);
            EnsureBaseForm();
            EnsurePluralForm();
            EnsureSingularForm();
        }
    }

    public override string[] StandardName => BaseForm;

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
            if (_singular != null && ((TokenString) _singular).Equals((TokenString) value))
                return;
            Ontology.EnsureUndefinedOrDefinedAsType(value, GetType());
            if (_singular != null) Trie.Store(_singular, null);
            _singular = value;
            Trie.Store(_singular, this);
            EnsurePluralForm();
            EnsureGerundForm();
        }
    }

    /// <summary>
    /// Add likely spellings of the gerund of this verb.
    /// They are stored as if they are plural inflections.
    /// </summary>
    private void EnsureGerundForm()
    {
        if (_gerundForm != null)
            return;
        EnsureBaseForm();
        foreach (var form in Inflection.GerundsOfVerb(_baseForm))
        {
            if (_gerundForm == null)
                _gerundForm = form;
            Trie.Store(form, this, true);
        }
    }

    private void EnsureBaseForm()
    {
        if (_baseForm != null)
            return;
        if (_gerundForm != null)
            _baseForm = Inflection.BaseFormOfGerund(_gerundForm);
        Debug.Assert(_plural != null || _singular != null || _baseForm != null);
        EnsurePluralForm();
        EnsureSingularForm();
        if (_baseForm != null)
            _baseForm = Inflection.ReplaceCopula(_plural, "be");
        EnsureGerundForm();
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
            if (_plural != null && ((TokenString) _plural).Equals((TokenString) value))
                return;
            Ontology.EnsureUndefinedOrDefinedAsType(value, GetType());
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
        if (_plural != null)
            return;
        if (_baseForm != null)
            PluralForm = Inflection.ReplaceCopula(_baseForm, "are"); 
        else
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

public enum VerbConjugation
{
    ThirdPerson,
    BaseForm,
    Gerund
};