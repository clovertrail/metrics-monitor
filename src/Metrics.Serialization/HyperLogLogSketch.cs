// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HyperLogLogSketch.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Online.Metrics.Serialization
{
    using global::Metrics.Services.Common;

    /// <summary>
    /// Represents a hyperloglog sketch.
    /// </summary>
    public class HyperLogLogSketch : IPoolTrackable
    {
        /// <summary>
        /// Maximum value of B.
        /// </summary>
        public const int MaxBValue = 14;

        /// <summary>
        /// Maximum value of B.
        /// </summary>
        public const int DefaultBValue = 10;

        /// <summary>
        /// The b value.
        /// </summary>
        private readonly byte bValue;

        /// <summary>
        /// The registers.
        /// </summary>
        private byte[] registers;

        /// <summary>
        /// Initializes a new instance of the <see cref="HyperLogLogSketch"/> class.
        /// </summary>
        /// <param name="bValue">HyperLogLog B value</param>
        /// <param name="initializeRegisters">Flag to indicate whether to initialize registers.</param>
        public HyperLogLogSketch(int bValue, bool initializeRegisters = true)
        {
            if (initializeRegisters)
            {
                this.registers = new byte[1 << bValue];
            }

            this.bValue = (byte)bValue;
        }

        /// <summary>
        /// Gets the B value for sketch.
        /// </summary>
        public byte BValue => this.bValue;

        /// <summary>
        /// Gets the registers associated with the sketch.
        /// </summary>
        public byte[] Registers
        {
            get
            {
                return this.registers;
            }

            protected set
            {
                this.registers = value;
            }
        }

        /// <inheritdoc />
        PoolObjectTrackingInfo IPoolTrackable.PoolObjectTrackingInfo { get; set; }

        /// <summary>
        /// Sets the register value for given key.
        /// </summary>
        /// <param name="key">Index value.</param>
        /// <returns>
        /// Register value at given key.
        /// </returns>
        public byte this[int key]
        {
            get
            {
                return this.registers[key];
            }

            set
            {
                this.registers[key] = value;
            }
        }

        /// <summary>
        /// Initializes the Sketch.
        /// </summary>
        public void Reset()
        {
            for (var i = 0; i < this.registers.Length; i++)
            {
                this.registers[i] = 0;
            }
        }

        /// <summary>
        /// Aggregates the given sketch to this sketch.
        /// </summary>
        /// <param name="other">Other sketch.</param>
        public void Aggregate(HyperLogLogSketch other)
        {
            if (other.BValue != this.bValue)
            {
                // We do not support aggregation of non-aligned buffers.
                return;
            }

            for (var i = 0; i < this.registers.Length; i++)
            {
                if (this.registers[i] < other.registers[i])
                {
                    this.registers[i] = other.registers[i];
                }
            }
        }
    }
}
