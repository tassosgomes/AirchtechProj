import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { AppLayout } from './layouts/AppLayout'
import { PrivateRoute } from './routes/PrivateRoute'
import { DashboardPage } from './pages/DashboardPage'
import { InventoryPage } from './pages/InventoryPage'
import { InventoryTimelinePage } from './pages/InventoryTimelinePage'
import { LoginPage } from './pages/LoginPage'
import { RequestDetailsPage } from './pages/RequestDetailsPage'
import { RequestNewPage } from './pages/RequestNewPage'

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route element={<PrivateRoute />}>
          <Route element={<AppLayout />}>
            <Route index element={<Navigate to="/dashboard" replace />} />
            <Route path="/dashboard" element={<DashboardPage />} />
            <Route path="/requests/new" element={<RequestNewPage />} />
            <Route path="/requests/:id" element={<RequestDetailsPage />} />
            <Route path="/inventory" element={<InventoryPage />} />
            <Route path="/inventory/:id/timeline" element={<InventoryTimelinePage />} />
          </Route>
        </Route>
        <Route path="*" element={<Navigate to="/dashboard" replace />} />
      </Routes>
    </BrowserRouter>
  )
}

export default App
