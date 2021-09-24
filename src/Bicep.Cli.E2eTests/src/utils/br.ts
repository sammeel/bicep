// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { v4 as uuidv4 } from "uuid";

export class BicepRegistryReferenceBuilder {
  readonly tagSuffix: string;

  constructor(readonly registry: string, readonly testArea: string) {
    const runId = uuidv4();

    // round down to full hour
    const creationDate = new Date();
    creationDate.setMinutes(0, 0, 0);

    // can't have colons in tag names and replace() only replaces the first occurrence
    const datePart = creationDate.toISOString().split(":").join("-");

    this.tagSuffix = `${datePart}_${runId}`;
  }

  public getRepository(name: string): string {
    return `${this.testArea}/${name}`;
  }

  public getBicepReference(name: string, tagPrefix: string): string {
    return `br:${this.registry}/${this.getRepository(name)}:${tagPrefix}_${
      this.tagSuffix
    }`;
  }
}
