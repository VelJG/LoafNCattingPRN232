import { readFileSync } from 'node:fs'
import { describe, expect, it } from 'vitest'

const customerStyles = readFileSync(
  'src/styles/v2-customer.css',
  'utf8',
)

describe('customer V2 header isolation', () => {
  it('resets the legacy floating-card styles on the header inner container', () => {
    const selector = '.customer-v2 .site-header__inner {'
    const start = customerStyles.indexOf(selector)
    const declarations = customerStyles.slice(start, customerStyles.indexOf('}', start))

    expect(start).toBeGreaterThan(-1)
    expect(declarations).toContain('margin: 0;')
    expect(declarations).toContain('border: 0;')
    expect(declarations).toContain('border-radius: 0;')
    expect(declarations).toContain('background: transparent;')
    expect(declarations).toContain('box-shadow: none;')
    expect(declarations).toContain('backdrop-filter: none;')
  })

  it('removes the legacy pill background from customer navigation links', () => {
    const selector = '.customer-v2 .customer-nav a {'
    const start = customerStyles.indexOf(selector)
    const declarations = customerStyles.slice(start, customerStyles.indexOf('}', start))

    expect(start).toBeGreaterThan(-1)
    expect(declarations).toContain('border-radius: 0;')
    expect(declarations).toContain('background: transparent;')
    expect(declarations).toContain('box-shadow: none;')
  })
})
