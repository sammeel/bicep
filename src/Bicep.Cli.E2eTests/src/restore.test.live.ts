// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/**
 * Live tests for "bicep restore".
 *
 * @group Live
 */

import { BicepRegistryReferenceBuilder } from "./utils/br";
import { invokingBicepCommand } from "./utils/command";
import {
  moduleCacheRoot,
  pathToCachedTsModuleFile,
  pathToExampleFile,
  emptyDir,
  expectFileExists,
  pathToTempFile,
} from "./utils/fs";
import fs from "fs";

async function emptyModuleCacheRoot() {
  await emptyDir(moduleCacheRoot);
}

describe("bicep restore", () => {
  beforeEach(emptyModuleCacheRoot);

  afterAll(emptyModuleCacheRoot);

  it("should restore template specs", () => {
    const exampleFilePath = pathToExampleFile("external-modules", "main.bicep");
    invokingBicepCommand("restore", exampleFilePath)
      .shouldSucceed()
      .withEmptyStdout();

    expectFileExists(
      pathToCachedTsModuleFile(
        "61e0a28a-63ed-4afc-9827-2ed09b7b30f3/bicep-ci/storageaccountspec-df/v1",
        "main.json"
      )
    );

    expectFileExists(
      pathToCachedTsModuleFile(
        "61e0a28a-63ed-4afc-9827-2ed09b7b30f3/bicep-ci/storageaccountspec-df/v2",
        "main.json"
      )
    );

    expectFileExists(
      pathToCachedTsModuleFile(
        "61e0a28a-63ed-4afc-9827-2ed09b7b30f3/bicep-ci/webappspec-df/1.0.0",
        "main.json"
      )
    );
  });

  it("should restore OCI artifacts", () => {
    const tempDir = pathToTempFile("restore");
    fs.mkdirSync(tempDir, { recursive: true });

    const builder = new BicepRegistryReferenceBuilder(
      "biceptestdf.azurecr.io",
      "restore"
    );

    const aksRef = builder.getBicepReference("aks", "v1");
    const aksPath = pathToExampleFile("101", "aks", "main.json");
    invokingBicepCommand(
      "publish",
      aksPath,
      "--target",
      aksRef
    ).shouldSucceed();

    const passthroughRef = builder.getBicepReference("passthrough", "v1");
    const passthroughPath = pathToExampleFile(
      "local-modules",
      "passthrough.bicep"
    );

    invokingBicepCommand(
      "publish",
      passthroughPath,
      "--target",
      passthroughRef
    ).shouldSucceed();
  });
});
