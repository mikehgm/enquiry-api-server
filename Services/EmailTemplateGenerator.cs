using Enquiry.API.Dtos;
using Enquiry.API.Models;
using System.Diagnostics.Contracts;

namespace Enquiry.API.Services
{
    public static class EmailTemplateGenerator
    {
        public static string GenerateTicketHtml(EnquiryModel enquiry, AppConfigDto config)
        {
            string pagadoLabel = enquiry.saldoPago.HasValue && enquiry.saldoPago == 0
                ? "<h1 style='text-align:center;'>PAGADO</h1>"
                : "";

            return $@"
        <div style='max-width:280px; margin:auto; font-family:monospace; background-color:#fefefe; border-radius:8px; border:1px solid #ccc; padding:1rem; text-align:center;'>
            <div style='background-color:#343a40; color:#ffffff; padding:10px; border-radius:6px 6px 0 0;'>
                <h4 style='margin:0;'>🧾 {config.TicketTitle}</h4>
            </div>
            <div style='padding:10px;'>
                <p>{config.CompanyName}</p>
                <p>{config.CompanyAddress}</p>
                <p>{config.CompanySucursal}</p>
                <p><strong>{config.Folio}:</strong></p>
                <h1>{enquiry.folio}</h1>
                <p><strong>{config.Cliente}:</strong> {enquiry.customerName}</p>
                <p><strong>{config.Telefono}:</strong> {enquiry.phone}</p>
                <p><strong>{config.Email}:</strong> {enquiry.email}</p>
                <hr style='margin:1rem 0;'>
                <p><strong>{config.Servicio}:</strong> {enquiry.enquiryTypeName}</p>
                <p><strong>{config.Mensaje}:</strong><br>{enquiry.message}</p>
                <hr style='margin:1rem 0;'>

                <p><strong>{config.Costo}:</strong> {enquiry.costo?.ToString("F2")}</p>

                <p><strong>{config.Anticipo}:</strong> {enquiry.anticipo?.ToString("F2")}</p>

                <p><strong>{config.Saldo}:</strong></p>
                <h1>${ enquiry.saldoPago?.ToString("F2")}</h1>
                {pagadoLabel}
                <hr>

                <p><strong>{config.Entrega}:</strong> {enquiry.dueDate?.ToString("dd/MM/yyyy")}</p>
                <p><strong>{config.Atendio}:</strong> {enquiry.createdBy}</p>
                <p><strong>{config.FechaCreacion}:</strong> {enquiry.createdDate.ToString("dd/MM/yyyy")}</p>
                <hr style='margin:1rem 0;'>
                <p>{config.CompanyThanks}</p>
                <p>{config.CompanyInfo}</p>
                <p><a href='https://www.dominio.com' target='_blank'>{config.CompanyWebsite}</a></p>
            </div>
        </div>";
        }

    }

}
