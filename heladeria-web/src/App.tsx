import { BrowserRouter } from 'react-router-dom'
import { ProveedorAutenticacion } from './auth/ProveedorAutenticacion'
import { ProveedorToast } from './components/toast/ProveedorToast'
import { EnrutadorAplicacion } from './rutas/EnrutadorAplicacion'

function App() {
  return (
    <BrowserRouter>
      <ProveedorToast>
        <ProveedorAutenticacion>
          <EnrutadorAplicacion />
        </ProveedorAutenticacion>
      </ProveedorToast>
    </BrowserRouter>
  )
}

export default App
