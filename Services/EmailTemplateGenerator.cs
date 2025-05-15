using Enquiry.API.Models;

namespace Enquiry.API.Services
{
    public static class EmailTemplateGenerator
    {
        public static string GenerateTicketHtml(EnquiryModel enquiry)
        {
            return $@"
        <div style='max-width:350px; margin:auto; font-family:monospace; background-color:#fefefe; border-radius:8px; border:1px solid #ccc; padding:1rem; text-align:center;'>
            <div style='background-color:#343a40; color:#ffffff; padding:10px; border-radius:6px 6px 0 0;'>
                <h4 style='margin:0;'>🧾 ENQUIRY TICKET</h4>
            </div>
            <div style='padding:10px;'>
                <p>Company Name</p>
                <p>Calle primera, el centro #1234, CP 12345, Ciudad, Estado</p>
                <p>Sucursal 123</p>
                <p><strong>Folio:</strong></p>
                <h3>{enquiry.folio}</h3>
                <p><strong>Cliente:</strong> {enquiry.customerName}</p>
                <p><strong>Teléfono:</strong> {enquiry.phone}</p>
                <p><strong>Email:</strong> {enquiry.email}</p>
                <hr style='margin:1rem 0;'>
                <p><strong>Servicio:</strong> {GetTypeLabel(enquiry.enquiryTypeId)}</p>
                <p><strong>Mensaje:</strong><br>{enquiry.message}</p>
                <hr style='margin:1rem 0;'>
                <p><strong>Costo:</strong></p>
                <h3>${enquiry.costo?.ToString("F2")}</h3>
                <hr style='margin:1rem 0;'>
                <p><strong>Entrega:</strong> {enquiry.dueDate?.ToString("dd/MM/yyyy")}</p>
                <p><strong>Atendió:</strong> {enquiry.createdBy}</p>
                <p><strong>Fecha de creación:</strong> {enquiry.createdDate.ToString("dd/MM/yyyy")}</p>
                <hr style='margin:1rem 0;'>
                <p>Gracias por su preferencia</p>
                <p>Para información de nuestros servicios puede visitar nuestra página web:</p>
                <p><a href='https://www.dominio.com' target='_blank'>www.dominio.com</a></p>
            </div>
        </div>";
        }

        private static string GetTypeLabel(int typeId)
        {
            return typeId switch
            {
                1 => "Servicio A",
                2 => "Servicio B",
                3 => "Servicio C",
                _ => "Otro servicio"
            };
        }
    }

}
