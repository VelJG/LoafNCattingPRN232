const featureCards = [
  {
    title: 'Menu ordering',
    description: 'Browse drinks, cakes, combos, and add items to cart.',
  },
  {
    title: 'Cat gallery',
    description: 'Show cat profiles, status, personality, and availability.',
  },
  {
    title: 'Reservations',
    description: 'Book tables, choose guests, and connect orders to visits.',
  },
  {
    title: 'Staff dashboard',
    description: 'Manage products, cats, tables, orders, and payments.',
  },
]

function App() {
  return (
    <main className="app-shell">
      <nav className="topbar" aria-label="Main navigation">
        <a className="brand" href="/">
          <span className="brand-mark" aria-hidden="true">
            🐾
          </span>
          <span>Loaf&apos;NCatting</span>
        </a>

        <div className="nav-links">
          <a href="#menu">Menu</a>
          <a href="#cats">Cats</a>
          <a href="#reservation">Reservation</a>
          <a href="#staff">Staff</a>
        </div>
      </nav>

      <section className="hero-section" aria-labelledby="hero-title">
        <div className="hero-copy">
          <p className="eyebrow">Cat café web app</p>
          <h1 id="hero-title">Order coffee, book tables, and meet the cats.</h1>
          <p className="hero-description">
            Basic frontend shell for the Loaf&apos;NCatting PRN232 project. This is ready for API-backed pages
            once the backend services/controllers are added.
          </p>

          <div className="hero-actions">
            <a className="button primary" href="#menu">
              Explore menu
            </a>
            <a className="button secondary" href="#reservation">
              Book a table
            </a>
          </div>
        </div>

        <div className="hero-card" aria-label="Today at Loaf'NCatting">
          <span className="hero-card-icon" aria-hidden="true">
            🐈
          </span>
          <h2>Today&apos;s café flow</h2>
          <ul>
            <li>Load products from ASP.NET Core Web API</li>
            <li>Reserve tables and create orders</li>
            <li>Cache hot menu/cat data on backend</li>
          </ul>
        </div>
      </section>

      <section className="features-grid" aria-label="Main app modules">
        {featureCards.map((feature) => (
          <article className="feature-card" key={feature.title}>
            <h2>{feature.title}</h2>
            <p>{feature.description}</p>
          </article>
        ))}
      </section>
    </main>
  )
}

export default App
