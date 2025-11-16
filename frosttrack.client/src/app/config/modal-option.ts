export class ModalOption {
  public static xl: {} = {
    ariaLabelledBy: 'modal-basic-title',
    backdrop: "static",
    size: "xl",
  };
  public static lg: {} = {
    ariaLabelledBy: 'modal-basic-title',
    backdrop: "static",
    size: "lg",
  };
  public static md: {} = {
    backdrop: "static",
    size: "lg",
    centered: true,
    windowClass: "modal-md",
  };
  public static auto: {} = {
    backdrop: "static",
    size: "lg",
    centered: true,
    windowClass: "modal-auto",
  };
  public static popup: {} = {
    backdrop: "static",
    size: "xl",
    centered: true,
    windowClass: "popupGuide",
  };
  public static testSchdlGuide: {} = {
    size: "xl",
    centered: true,
    windowClass: "testScheduleGuide",
  };
  
  public static lightBox: {} = {
    backdrop: "static",
    size: "xl",
    windowClass: "modal-lightbox",
    centered: false,
  };
  public static sm: {} = {
    backdrop: "static",
    size: "sm",
    centered: true,
  };
}
