// Windows: the terminal may sit in "e:\..." while Node resolves modules to "E:\..." → Vitest loads two copies and fails with
// "Cannot read properties of undefined (reading 'config')". Run Vitest in a child process with an upper-case drive letter.
import { spawnSync } from 'node:child_process'
import { fileURLToPath } from 'node:url'

const upperDrive = (path) => path.replace(/^[a-z]:/, (drive) => drive.toUpperCase())
const cwd = upperDrive(process.cwd())
const bin = upperDrive(fileURLToPath(new URL('../node_modules/vitest/vitest.mjs', import.meta.url)))

const { status } = spawnSync(process.execPath, [bin, ...process.argv.slice(2)], { cwd, stdio: 'inherit' })
process.exit(status ?? 1)
