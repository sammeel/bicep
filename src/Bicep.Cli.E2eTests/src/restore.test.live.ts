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
  pathToCachedBrModuleFile,
} from "./utils/fs";
import fs from "fs";
import path from "path";

async function emptyModuleCacheRoot() {
  await emptyDir(moduleCacheRoot);
}

describe("bicep restore", () => {
  beforeEach(emptyModuleCacheRoot);

  //afterAll(emptyModuleCacheRoot);

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

    const storageRef = builder.getBicepReference("storage", "v1");
    const storagePath = pathToExampleFile("local-modules", "storage.bicep");
    invokingBicepCommand(
      "publish",
      storagePath,
      "--target",
      storageRef
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

    const bicepPath = path.join(tempDir, "main.bicep");
    const bicep = `
module passthrough '${passthroughRef}' = {
  name: 'passthrough'
  params: {
    text: 'hello'
    number: 42
  }
}

module storage '${storageRef}' = {
  name: 'storage'
  params: {
    name: passthrough.outputs.result
  }
}

output blobEndpoint string = storage.outputs.blobEndpoint
    `;

    fs.writeFileSync(bicepPath, bicep);

    invokingBicepCommand("restore", bicepPath).shouldSucceed();

    const moduleFiles = ["lock", "main.json", "manifest", "metadata"];

    moduleFiles.forEach((fileName) => {
      const filePath = pathToCachedBrModuleFile(
        builder.registry,
        "restore$passthrough",
        `v1_${builder.tagSuffix}$4002000`,
        fileName
      );
      console.log(filePath);
      expectFileExists(filePath);
    });

    moduleFiles.forEach((fileName) => {
      const filePath = pathToCachedBrModuleFile(
        builder.registry,
        "restore$storage",
        `v1_${builder.tagSuffix}$4002000`,
        fileName
      );
      console.log(filePath);
      expectFileExists(filePath);
    });
  });
});
