﻿#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Invention.cs" company="Ian Horswill">
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

using CatSAT;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// Represents the output of the generator.
/// This contains a model, which maps propositions to truth values
/// </summary>
public class Invention
{
    /// <summary>
    /// The output from the "imagine" command that created this invention.
    /// </summary>
    public List<Individual> Individuals => Generator.Individuals;

    public Generator Generator;
    /// <summary>
    /// The model of Problem most recently generated by CatSAT
    /// </summary>
    public Solution Model;

    public Invention(Generator generator, Solution model)
    {
        Generator = generator;
        Model = model;
    }

    #region Description generation
    /// <summary>
    /// A textual description of the Individual's attributes within Model.
    /// </summary>
    public string Description(Individual i, string startEmphasis="", string endEmphasis="")
    {
        // Properties we shouldn't surface because they're implicit or already surfaced.
        var suppressedProperties = new List<Property>();
        var name = NameString(i, suppressedProperties);

        var adjectivalPhrases = AdjectivesDescribing(i).Select(a => a.StandardName).Cast<IEnumerable<string>>().ToList();
        // Add commas after all but the last adjectival phrase
        for (int n = 0; n < adjectivalPhrases.Count - 1; n++)
            adjectivalPhrases[n] = adjectivalPhrases[n].Append(",");
        var result = adjectivalPhrases.SelectMany(list => list).Append(startEmphasis).Concat(MostSpecificNouns(i).SelectMany(n => n.StandardName)).Append(endEmphasis).Prepend("a").Untokenize();
        foreach (var pair in i.Properties)
        {
            var property = pair.Key;
            if (suppressedProperties.Contains(property))
                continue;

            var propertyName = property.Text;
            var prop = pair.Value;
            var value = FormatPropertyValue(prop, Model[prop]);
            result = $"{result}, {propertyName} {value}";
        }

        return $"{startEmphasis}{name}{endEmphasis} is {result}";
    }

    // ReSharper disable once UnusedParameter.Local
    private string FormatPropertyValue(Variable prop, object value)
    {
        switch (value)
        {
            case float f:
                return Math.Round(f).ToString(CultureInfo.InvariantCulture);

            default:
                return value.ToString();
        }
    }

    /// <summary>
    /// All Adjectives that are true of the individual in Model.
    /// </summary>
    public IEnumerable<Adjective> AdjectivesDescribing(Individual i)
    {
        var kinds = TrueKinds(i);
        var relevantAdjectives = kinds.SelectMany(k => k.RelevantAdjectives.Concat(k.AlternativeSets.SelectMany(a => a.Alternatives).Select(a => a.Concept))).Where(a => a is Adjective).Cast<Adjective>().Distinct();
        return relevantAdjectives.Where(a => IsA(i, a));
    }

    /// <summary>
    /// Finds the minima of the sub-lattice of nouns satisfied by this individual.
    /// Translation: every noun that's true of ind but that doesn't have a more specific noun that's
    /// also true of it.  We suppress the more general nouns because they're implied by the truth of
    /// the more specified ones.
    /// </summary>
    public IEnumerable<CommonNoun> MostSpecificNouns(Individual ind)
    {
        var nouns = new HashSet<CommonNoun>();

        void MaybeAddNoun(CommonNoun n)
        {
            if (!IsA(ind, n) || nouns.Contains(n))
                return;

            nouns.Add(n);
            foreach (var sub in n.Subkinds)
                MaybeAddNoun(sub);
        }

        foreach (var n in ind.Kinds)
            MaybeAddNoun(n);

        // All the nouns that already have a more specific noun in nouns
        // We make a separate table of these rather than removing them from nouns
        // in order to avoid mutating a table while iterating over it, which is outlawed by
        // foreach and likely to be very buggy in this instance even if foreach would let us do it.
        var redundant = new HashSet<CommonNoun>();

        void MarkRedundant(CommonNoun n)
        {
            if (redundant.Contains(n))
                return;

            redundant.Add(n);

            foreach (var sup in n.Superkinds)
                MarkRedundant(sup);
        }

        foreach (var n in nouns)
            foreach (var sup in n.Superkinds)
                MarkRedundant(sup);

        return nouns.Where(n => !redundant.Contains(n));
    }

    public string NameString(Individual i, List<Property> suppressedProperties = null)
    {
        var nameProperty = i.NameProperty();
        if (nameProperty != null)
        {
            suppressedProperties?.Add(nameProperty);
            return Model[i.Properties[nameProperty]].ToString();
        }

        Debug.AssertFormat(i.Kinds.Count > 0, "NameString({0}): individual has no kinds?", i);
        var kind = i.Kinds[0];
        return kind.NameTemplate != null ? FormatNameFromTemplate(i, suppressedProperties, kind) : i.Text;
    }

    private string FormatNameFromTemplate(Individual i, List<Property> suppressedProperties, CommonNoun kind)
    {
        var b = new StringBuilder();
        var t = kind.NameTemplate;
        var firstOne = true;

        for (var n = 0; n < t.Length; n++)
        {
            if (firstOne)
                firstOne = false;
            else
                b.Append(' ');

            var token = t[n];
            if (token == "[")
            {
                // Get a property name
                var start = n+1;
                var end = Array.IndexOf(t, "]", n);
                if (end < 0)
                    end = t.Length;
                var propertyName = new string[end - start];
                Array.Copy(t, start, propertyName, 0, end - start);
                // Find the property
                var property = kind.PropertyNamed(propertyName);
                if (property == null)
                    b.Append($"<unknown property {propertyName.Untokenize()}>");
                else
                {
                    // Print its value
                    b.Append(Model[i.Properties[property]]);
                    suppressedProperties?.Add(property);
                }

                n = end;
            }
            else
                b.Append(token);
        }

        return b.ToString();
    }
    #endregion

    #region Model testing
    /// <summary>
    /// True if the concept with the specified name applies to the individual in the current Model.
    /// </summary>
    /// <param name="i">Individual to test</param>
    /// <param name="name">Name of concept to test</param>
    /// <returns>True if Individual is an instance of the named concept.</returns>
    public bool IsA(Individual i, params string[] name) => IsA(i, (CommonNoun) Noun.Find(name));

    /// <summary>
    /// True if concept applies to individual in the current Model.
    /// </summary>
    /// <param name="i">Individual to test</param>
    /// <param name="c">Concept to test the truth of</param>
    /// <returns>True if i is an instance of c in the current Model</returns>
    public bool IsA(Individual i, MonadicConcept c) => Model[Generator.IsA(i, c)];

    public bool Holds(Verb v, Individual i1, Individual i2) => Model[Generator.Holds(v, i1, i2)];

    public bool Holds(string verb, Individual i1, Individual i2) => Holds(Verb.Find(verb), i1, i2);

    public IEnumerable<Tuple<Verb, Individual, Individual>>  Relationships
    {
        get
        {
            var verbs = Verb.AllVerbs.ToArray();
            foreach (var i1 in Individuals)
            foreach (var i2 in Individuals)
            foreach (var v in verbs)
                if (Generator.CanBeA(i1, v.SubjectKind) && Generator.CanBeA(i2, v.ObjectKind) && Holds(v, i1, i2))
                    yield return new Tuple<Verb, Individual, Individual>(v, i1, i2);
        }
    }

    /// <summary>
    /// All the kinds that apply to the individual in the current Model
    /// </summary>
    /// <param name="ind">Individual to look up the kinds of</param>
    /// <returns>All kinds that apply to individual</returns>
    public List<CommonNoun> TrueKinds(Individual ind)
    {
        var result = new List<CommonNoun>();

        void AddKindsDownward(List<CommonNoun> list, Individual i, CommonNoun k)
        {
            list.AddNew(k);
            foreach (var sub in k.Subkinds)
                if (IsA(i, sub))
                {
                    AddKindsDownward(list, i, sub);
                    return;
                }
        }

        void AddKindsUpward(List<CommonNoun> list, Individual i, CommonNoun k)
        {
            list.AddNew(k);
            foreach (var super in k.Superkinds)
                if (IsA(i, super))
                    AddKindsUpward(list, i, super);
        }

        foreach (var k in ind.Kinds)
            if (IsA(ind, k))
            {
                AddKindsUpward(result, ind, k);
                AddKindsDownward(result, ind, k);
            }

        return result;
    }
    #endregion
}
