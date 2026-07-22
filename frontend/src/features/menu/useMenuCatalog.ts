import { useEffect, useState } from 'react'
import { catalogRepository } from '../../services/catalogRepository'
import type { Category, Product } from '../../types/models'

export function useMenuCatalog() {
  const [keyword, setKeyword] = useState('')
  const [categories, setCategories] = useState<Category[]>([])
  const [categoryId, setCategoryId] = useState<number | undefined>()
  const [products, setProducts] = useState<Product[]>([])
  const [categoriesFailed, setCategoriesFailed] = useState(false)
  const [productsFailed, setProductsFailed] = useState(false)
  const [loading, setLoading] = useState(true)
  const [retryKey, setRetryKey] = useState(0)

  useEffect(() => {
    let active = true
    setCategoriesFailed(false)

    catalogRepository
      .listCategories()
      .then((result) => {
        if (active) setCategories(result)
      })
      .catch(() => {
        if (!active) return
        setCategories([])
        setCategoriesFailed(true)
      })

    return () => {
      active = false
    }
  }, [retryKey])

  useEffect(() => {
    let active = true
    setLoading(true)
    setProductsFailed(false)

    catalogRepository
      .listProducts({ keyword, categoryId })
      .then((result) => {
        if (!active) return
        setProducts(result)
        setLoading(false)
      })
      .catch(() => {
        if (!active) return
        setProducts([])
        setProductsFailed(true)
        setLoading(false)
      })

    return () => {
      active = false
    }
  }, [categoryId, keyword, retryKey])

  return {
    keyword,
    setKeyword,
    categories,
    categoryId,
    setCategoryId,
    products,
    categoriesFailed,
    productsFailed,
    loading,
    retry: () => setRetryKey((value) => value + 1),
  }
}
