### General idea

We need a **deploy key (SSH key for this repo only)** that:
- has its public part in GitHub (`Deploy keys` on `denis-peshkov/Cross.Identity`)
- has its private part in Azure DevOps, where the pipeline installs it before `git submodule update`.

---

### Step 1. Create an SSH key (deploy key)

On your machine:

```bash
ssh-keygen -t ed25519 -C "azure-devops-deploy-key" -f ./cross-identity-deploy-key
# passphrase can be left empty for CI
```

You will get two files:
- `cross-identity-deploy-key` — private
- `cross-identity-deploy-key.pub` — public

---

### Step 2. Add the key to GitHub as a Deploy key (read-only)

1. Go to GitHub: `denis-peshkov/Cross.Identity`
2. `Settings` → `Deploy keys` → `Add deploy key`
3. Title: `azure-devops-readonly`
4. Key: contents of `cross-identity-deploy-key.pub`
5. **Do not** check "Allow write access" (must be read-only)
6. Save.

---

### Step 3. Add the private key to Azure DevOps

Option via Secure Files + `InstallSSHKey` (simple and explicit):

1. In Azure DevOps → your project → `Pipelines` → `Library` → `Secure files`
2. `Upload` → upload `cross-identity-deploy-key`
3. Name it, for example, `cross-identity-deploy-key`

---

### Step 4. Update pipeline YAML

Before `checkout: self` you need to install the key and enable submodules.

```yaml
steps:
  # Install SSH key
  - task: InstallSSHKey@0
    displayName: 'Install SSH key for Cross.Identity'
    inputs:
      knownHostsEntry: 'github.com ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAAIOMqqnkVzrm0SdG6UOoqKLsabgH5C9okWi0dh2l9GKJl'
      sshPublicKey: ''                                # can be left empty
      sshKeySecureFile: 'cross-identity-deploy-key'   # name from Secure Files

  # Clone repository with submodules
  - checkout: self
    submodules: recursive
    persistCredentials: true
```

`knownHostsEntry` can be obtained from:

```bash
ssh-keyscan -t ed25519 github.com
```

(paste the entire line).

Important:
- `.gitmodules` should already have `url = git@github.com:denis-peshkov/Cross.Identity.git` — **do not change**.
- In `checkout: self` you must have `submodules: recursive`, otherwise the submodule will not be fetched.

---

### Step 5. Verify

- Run the pipeline in Azure DevOps.
- In the `InstallSSHKey` step log, verify that the key was installed.
- In the `checkout` step there should no longer be `Permission denied (publickey)`.

If you want, send a snippet of the current pipeline YAML — I can insert the ready-made block directly into it.
