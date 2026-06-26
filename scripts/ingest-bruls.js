#!/usr/bin/env node
const fs = require('fs')
const path = require('path')
const matter = require('gray-matter')

const input = path.resolve(__dirname, '..', 'src', 'knowledgebase', 'BRULS.md')
const outDir = path.resolve(__dirname, '..', 'tmp')
if (!fs.existsSync(outDir)) fs.mkdirSync(outDir, { recursive: true })

const text = fs.readFileSync(input, 'utf8')

const fileParsed = matter(text)
const collection = fileParsed.data || {}
const body = fileParsed.content

// Parse blocks shaped like:
// ```yaml
// id: Rule-101
// ...
// ```
// ## Rule-101: ...
// <rule body>
const ruleRegex = /```yaml\s*\n([\s\S]*?)\n```\s*\n(##\s+Rule-\d+:[^\n]+)\n([\s\S]*?)(?=\n```yaml\s*\n|$)/g

const docs = []
for (const match of body.matchAll(ruleRegex)) {
  const yamlBlock = match[1].trim()
  const heading = match[2].trim()
  const content = match[3].trim()

  const parsedMeta = matter(`---\n${yamlBlock}\n---`)
  const metadata = parsedMeta.data || {}
  if (!metadata.id) continue

  docs.push({
    id: metadata.id,
    heading,
    metadata,
    collection,
    content,
  })
}

const outPath = path.join(outDir, 'bruls-chunks.json')
fs.writeFileSync(outPath, JSON.stringify(docs, null, 2), 'utf8')
console.log('Wrote', outPath, 'with', docs.length, 'docs')
