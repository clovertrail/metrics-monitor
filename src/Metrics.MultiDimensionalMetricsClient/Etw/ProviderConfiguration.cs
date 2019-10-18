// -----------------------------------------------------------------------
// <copyright file="ProviderConfiguration.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Cloud.Metrics.Client.Metrics.Etw
{
    using System;

    /// <summary>
    /// Type that reads the settings of a single provider from a configuration file.
    /// </summary>
    /// <remarks>
    /// The value of a provider key is expected to be in the following format:
    /// <para/>&lt;GUID&gt;[,&lt;parameterPair&gt;]*<para/>
    /// In which a parameterPair has the following format "parameterName:parameterValue".
    /// Below is a list of some valid provider configurations with respective keys:
    /// <para/>
    /// Provider1 = {D857C50C-9002-4852-94A4-7264063CF38D}
    /// Provider2 = {9FD91669-452C-4B25-AD5B-5322D511DA65},level:Informational,KeywordsAny:0x1f0
    /// Provider3 = {B7F33BAA-E45A-4FCF-8389-FA103A2AC23C},KeywordsAll:0x5
    /// <para>
    /// None of the parameters is mandatory, for all of them a default value is provided if not
    /// specified on the configuration.
    /// </para>
    /// </remarks>
    internal sealed class ProviderConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProviderConfiguration"/> class with the specific
        /// values given by the caller.
        /// </summary>
        /// <param name="id">
        /// The id that identifies the provider.
        /// </param>
        /// <param name="level">
        /// The logging level from which events should be logged.
        /// </param>
        /// <param name="keywordsAny">
        /// The "keywords any" value to be used when enabling this provider.
        /// </param>
        /// <param name="keywordsAll">
        /// The "keywords all" value to be used when enabling this provider.
        /// </param>
        public ProviderConfiguration(Guid id, EtwTraceLevel level, long keywordsAny, long keywordsAll)
        {
            this.Id = id;
            this.Level = level;
            this.KeywordsAny = keywordsAny;
            this.KeywordsAll = keywordsAll;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProviderConfiguration"/> class.
        /// </summary>
        private ProviderConfiguration()
        {
        }

        /// <summary>
        /// Gets the unique id of the provider.
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// Gets the level from which events should be logged.
        /// </summary>
        public EtwTraceLevel Level { get; private set; }

        /// <summary>
        /// Gets the match any keyword value to be used when enabling this provider.
        /// </summary>
        /// <see href="http://msdn.microsoft.com/en-us/library/windows/desktop/dd392305(v=vs.85).aspx"/>
        public long KeywordsAny { get; private set; }

        /// <summary>
        /// Gets the match all keyword value to be used when enabling this provider.
        /// </summary>
        /// <see href="http://msdn.microsoft.com/en-us/library/windows/desktop/dd392305(v=vs.85).aspx"/>
        public long KeywordsAll { get; private set; }

        /// <summary>
        /// Overriding ToString method to help tests and debugging.
        /// </summary>
        /// <returns>
        /// The <see cref="string"/> representing the provider configuration.
        /// </returns>
        public override string ToString()
        {
            return string.Format(
                "Id: {0} Level: {1} KeywordsAny: 0x{2} KeywordsAll: 0x{3}",
                this.Id.ToString("B"),
                this.Level.ToString(),
                this.KeywordsAny.ToString("X16"),
                this.KeywordsAll.ToString("X16"));
        }

        /// <summary>
        /// Creates a new instance cloned from the current one.
        /// </summary>
        /// <returns>
        /// The new <see cref="ProviderConfiguration"/> instance cloned from the current
        /// instance.
        /// </returns>
        public ProviderConfiguration Clone()
        {
            var clone = new ProviderConfiguration();
            clone.Id = this.Id;
            clone.Level = this.Level;
            clone.KeywordsAny = this.KeywordsAny;
            clone.KeywordsAll = this.KeywordsAll;

            return clone;
        }

        /// <summary>
        /// The merge the provider configuration with other instance when the provider
        /// is being enabled.
        /// </summary>
        /// <param name="otherConfiguration">
        /// The other configuration instance to be merged for enable.
        /// </param>
        public void MergeForEnable(ProviderConfiguration otherConfiguration)
        {
            if (this.Id == otherConfiguration.Id)
            {
                this.Level = this.Level > otherConfiguration.Level ? this.Level : otherConfiguration.Level;
                this.KeywordsAny |= otherConfiguration.KeywordsAny;
                this.KeywordsAll &= otherConfiguration.KeywordsAll;
            }
        }
    }
}