### Общая идея

Нам нужен **deploy key (SSH‑ключ только для этого репо)**, который:
- публичной частью лежит в GitHub (`Deploy keys` у `denis-peshkov/Cross.Identity`)
- приватной частью — в Azure DevOps, где pipeline ставит его перед `git submodule update`.

---

### Шаг 1. Создать SSH‑ключ (deploy key)

На своей машине:

```bash
ssh-keygen -t ed25519 -C "azure-devops-deploy-key" -f ./cross-identity-deploy-key
# passphrase можно оставить пустой для CI
```

Получите два файла:
- `cross-identity-deploy-key` — приватный
- `cross-identity-deploy-key.pub` — публичный

---

### Шаг 2. Добавить ключ в GitHub как Deploy key (read‑only)

1. Зайти в GitHub: `denis-peshkov/Cross.Identity`
2. `Settings` → `Deploy keys` → `Add deploy key`
3. Title: `azure-devops-readonly`
4. Key: содержимое `cross-identity-deploy-key.pub`
5. **Не** ставьте галку “Allow write access” (должен быть read‑only)
6. Сохранить.

---

### Шаг 3. Добавить приватный ключ в Azure DevOps

Вариант через Secure Files + `InstallSSHKey` (простой и явный):

1. В Azure DevOps → ваш проект → `Pipelines` → `Library` → `Secure files`
2. `Upload` → загрузить `cross-identity-deploy-key`
3. Назвать, например, `cross-identity-deploy-key`

---

### Шаг 4. Обновить pipeline YAML

Перед `checkout: self` нужно поставить ключ и включить сабмодули.

```yaml
steps:
  # Устанавливаем SSH ключ
  - task: InstallSSHKey@0
    displayName: 'Install SSH key for Cross.Identity'
    inputs:
      knownHostsEntry: 'github.com ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAAIOMqqnkVzrm0SdG6UOoqKLsabgH5C9okWi0dh2l9GKJl'
      sshPublicKey: ''                                # можно оставить пустым
      sshKeySecureFile: 'cross-identity-deploy-key'   # имя из Secure Files

  # Клонируем репозиторий с сабмодулями
  - checkout: self
    submodules: recursive
    persistCredentials: true
```

`knownHostsEntry` можно взять из:

```bash
ssh-keyscan -t ed25519 github.com
```

(вставляете строку целиком).

Важно:
- В `.gitmodules` уже должен быть `url = git@github.com:denis-peshkov/Cross.Identity.git` — **не меняем**.
- В `checkout: self` обязательно `submodules: recursive`, иначе сабмодуль не подтянется.

---

### Шаг 5. Проверить

- Запустить pipeline в Azure DevOps.
- В логе шага `InstallSSHKey` убедиться, что ключ установлен.
- В шаге `checkout` больше не должно быть `Permission denied (publickey)`.

Если хотите, пришлите кусок актуального YAML pipeline — могу прямо в нем вставить готовый блок.
