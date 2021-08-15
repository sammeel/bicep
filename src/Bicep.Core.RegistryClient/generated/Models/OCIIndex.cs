// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// <auto-generated/>

#nullable disable

using System.Collections.Generic;
using Azure.Core;

namespace Bicep.Core.RegistryClient.Models
{
    /// <summary> Returns the requested OCI index file. </summary>
    internal partial class OCIIndex : Manifest
    {
        /// <summary> Initializes a new instance of OCIIndex. </summary>
        /// <param name="schemaVersion"> Schema version. </param>
        internal OCIIndex(int schemaVersion) : base(schemaVersion)
        {
            Manifests = new ChangeTrackingList<ManifestListAttributes>();
            MediaType = "application/vnd.oci.image.index.v1+json";
        }

        /// <summary> Initializes a new instance of OCIIndex. </summary>
        /// <param name="schemaVersion"> Schema version. </param>
        /// <param name="mediaType"> Media type for this Manifest. </param>
        /// <param name="manifests"> List of OCI image layer information. </param>
        /// <param name="annotations"> Additional information provided through arbitrary metadata. </param>
        internal OCIIndex(int schemaVersion, string mediaType, IReadOnlyList<ManifestListAttributes> manifests, Annotations annotations) : base(schemaVersion, mediaType)
        {
            Manifests = manifests;
            Annotations = annotations;
            MediaType = mediaType ?? "application/vnd.oci.image.index.v1+json";
        }

        /// <summary> List of OCI image layer information. </summary>
        public IReadOnlyList<ManifestListAttributes> Manifests { get; }
        /// <summary> Additional information provided through arbitrary metadata. </summary>
        public Annotations Annotations { get; }
    }
}