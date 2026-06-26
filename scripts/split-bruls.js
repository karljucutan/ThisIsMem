#!/usr/bin/env node
const fs = require('fs')
const path = require('path')

function parseSimpleYamlBlock(yaml) {
  const result = {}
  let currentParent = null

  for (const rawLine of yaml.split(/\r?\n/)) {
    const line = rawLine.trimEnd()
    if (!line.trim()) continue

    if (/^\w[\w-]*:\s*$/.test(line)) {
      currentParent = line.replace(':', '').trim()
      if (!result[currentParent]) result[currentParent] = {}
      continue
    }

    const childMatch = line.match(/^\s{2,}(\w[\w-]*):\s*(.*)$/)
    if (childMatch && currentParent) {
      const key = childMatch[1]
      const value = childMatch[2].trim().replace(/^"|"$/g, '')
      result[currentParent][key] = value
      continue
    }

    const kv = line.match(/^(\w[\w-]*):\s*(.*)$/)
    if (!kv) continue

    const key = kv[1]
    const rawValue = kv[2].trim()

    if (rawValue.startsWith('[') && rawValue.endsWith(']')) {
      const inner = rawValue.slice(1, -1).trim()
      result[key] = inner ? inner.split(',').map((x) => x.trim()) : []
      currentParent = null
      continue
    }

    if (/^\d+$/.test(rawValue)) {
      result[key] = Number(rawValue)
      currentParent = null
      continue
    }

    result[key] = rawValue.replace(/^"|"$/g, '')
    currentParent = null
  }

  return result
}

function toYaml(obj) {
  const lines = []
  for (const [key, value] of Object.entries(obj)) {
    if (Array.isArray(value)) {
      lines.push(`${key}: [${value.join(', ')}]`)
      continue
    }
    if (value && typeof value === 'object') {
      lines.push(`${key}:`)
      for (const [childKey, childValue] of Object.entries(value)) {
        lines.push(`  ${childKey}: ${childValue}`)
      }
      continue
    }
    lines.push(`${key}: ${value}`)
  }
  return lines.join('\n')
}

const root = path.resolve(__dirname, '..')
const input = path.join(root, 'src', 'knowledgebase', 'BRULS.md')
const groupedDir = path.join(root, 'src', 'knowledgebase')
const standaloneDir = path.join(root, 'src', 'knowledgebase', 'rules')

if (!fs.existsSync(input)) {
  console.error('Missing input:', input)
  process.exit(1)
}

if (!fs.existsSync(standaloneDir)) {
  fs.mkdirSync(standaloneDir, { recursive: true })
}

const text = fs.readFileSync(input, 'utf8')
const collectionMatch = text.match(/^---\s*\r?\n([\s\S]*?)\r?\n---\s*\r?\n([\s\S]*)$/)
if (!collectionMatch) {
  console.error('Unable to parse collection frontmatter in BRULS.md')
  process.exit(1)
}
const collection = parseSimpleYamlBlock(collectionMatch[1])
const body = collectionMatch[2]

const ruleRegex = /```yaml\s*\n([\s\S]*?)\n```\s*\n(##\s+Rule-\d+:[^\n]+)\n([\s\S]*?)(?=\n```yaml\s*\n|$)/g

const rules = []
for (const match of body.matchAll(ruleRegex)) {
  const yamlBlock = match[1].trim()
  const heading = match[2].trim()
  const content = match[3].trim()

  const metadata = parseSimpleYamlBlock(yamlBlock)
  if (!metadata.id || !metadata.canonicalSlug) continue

  const headingTitle = heading.replace(/^##\s+Rule-\d+:\s*/, '').trim()
  const domain = metadata.category || 'General'

  rules.push({
    metadata,
    heading,
    headingTitle,
    content,
    domain,
  })
}

if (rules.length === 0) {
  console.error('No rules parsed from BRULS.md')
  process.exit(1)
}

const byDomain = new Map()
for (const rule of rules) {
  if (!byDomain.has(rule.domain)) byDomain.set(rule.domain, [])
  byDomain.get(rule.domain).push(rule)
}

for (const [domain, domainRules] of byDomain.entries()) {
  const groupedFrontmatter = {
    id: `${collection.id || 'BRULS'}-${domain}-Grouped-v1`,
    title: `${collection.source || 'BRULS'} ${domain} Grouped Rules`,
    type: 'collection',
    source: collection.source || 'BRULS',
    domain,
    created: collection.created || '2026-06-26',
    lastReviewed: collection.lastReviewed || '2026-06-26',
    version: 1,
    author: collection.author || { name: 'Unknown' },
  }

  let out = '---\n'
  out += toYaml(groupedFrontmatter)
  out += '\n---\n\n'
  out += `## ${(collection.source || 'BRULS')} ${domain} Grouped Rules\n\n`
  out += '## Group Summary\n\n'
  out += `This grouped file contains related ${domain} rules organized for progressive disclosure.\n\n`

  for (const rule of domainRules) {
    const yaml = [
      `id: ${rule.metadata.id}`,
      `title: ${rule.metadata.title}`,
      `category: ${rule.metadata.category}`,
      `tags: [${(rule.metadata.tags || []).join(', ')}]`,
      `created: ${rule.metadata.created}`,
      `lastReviewed: ${rule.metadata.lastReviewed}`,
      `version: ${rule.metadata.version}`,
      `canonicalSlug: ${rule.metadata.canonicalSlug}`,
    ].join('\n')

    out += '```yaml\n'
    out += `${yaml}\n`
    out += '```\n\n'
    out += `${rule.heading}\n\n`
    out += `${rule.content.trim()}\n\n`
  }

  const groupedPath = path.join(groupedDir, `BRULS.${domain}.Grouped.md`)
  fs.writeFileSync(groupedPath, out.trimEnd() + '\n', 'utf8')
}

for (const rule of rules) {
  const frontmatter = {
    id: rule.metadata.id,
    title: rule.metadata.title,
    type: 'rule',
    source: collection.source || 'BRULS',
    category: rule.metadata.category,
    domain: rule.metadata.category,
    tags: rule.metadata.tags || [],
    created: rule.metadata.created,
    lastReviewed: rule.metadata.lastReviewed,
    version: rule.metadata.version,
    canonicalSlug: rule.metadata.canonicalSlug,
  }

  let out = '---\n'
  out += toYaml(frontmatter)
  out += '\n---\n\n'
  out += `${rule.heading}\n\n`
  out += `${rule.content.trim()}\n`

  const fileName = `${rule.metadata.id}-${rule.metadata.canonicalSlug.replace(/^rule-\d+-/, '')}.md`
  const outPath = path.join(standaloneDir, fileName)
  fs.writeFileSync(outPath, out, 'utf8')
}

console.log('Generated', byDomain.size, 'grouped files and', rules.length, 'standalone files from BRULS.md')
